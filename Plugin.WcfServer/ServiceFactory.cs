using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
#if NETFRAMEWORK
using System.ServiceModel;
using System.ServiceModel.Description;
#else
using CoreWCF;
using CoreWCF.Description;
using CommunicationState = System.ServiceModel.CommunicationState;
using ServiceHost = Plugin.WcfServer.CoreWcfServiceHost;
using ServiceEndpoint = CoreWCF.Description.ServiceEndpoint;
#endif
using Plugin.WcfServer.Services;
using Plugin.WcfServer.Services.Control;

namespace Plugin.WcfServer
{
	internal class ServiceFactory : IDisposable
	{
		//TODO: Switch to ConcurrentDictionary
		public static readonly Dictionary<Int32, PluginsServiceProxy> Proxies = new Dictionary<Int32, PluginsServiceProxy>();

		private IpcSingleton _ipc;

		private ServiceHost _controlHost;
		private ServiceHost _restHost;
		private ServiceHost _soapHost;

		private ControlServiceProxy _controlProxy;
		private String _hostUrl;
		private PluginSettings.ServiceType _serviceType;
		private Timer _ping;
		private readonly Object ObjLock = new Object();

		/// <summary>Base address for IPC connection</summary>
		/// <remarks>External URL is used for identifier because it is the common part</remarks>
		private String BaseAddress => "net.pipe://" + Environment.MachineName + "/Plugin.WcfServer" + this._hostUrl.GetHashCode().ToString();
		private String BaseControlAddress => this.BaseAddress + "/Control";

		public Boolean IsHost => this._controlHost != null;

		public event EventHandler<EventArgs> Connected;

		public CommunicationState State
		{
			get
			{
				if(this._controlHost != null)
				{
					if(this._restHost != null && this._controlHost.State != this._restHost.State)
						return CommunicationState.Faulted;//Return error if they are not both started
					if(this._soapHost != null && this._controlHost.State != this._soapHost.State)
						return CommunicationState.Faulted;//Return error if they are not both started

					// CoreWCF.ServiceHost uses CoreWCF.CommunicationState
					// We need to convert it to System.ServiceModel.CommunicationState for consistency
					return (CommunicationState)(Int32)this._controlHost.State;
				} else if(this._controlProxy != null)
					return this._controlProxy.State;
				else return CommunicationState.Closed;
			}
		}

		/// <summary>Get list of addresses under which hosts are running</summary>
		/// <returns></returns>
		public IEnumerable<String> GetHostEndpoints()
		{
			if(this._restHost != null)
				foreach(ServiceEndpoint addr in this._restHost.Description.Endpoints)
					yield return addr.Address.ToString();
			if(this._soapHost != null)
				foreach(ServiceEndpoint addr in this._soapHost.Description.Endpoints)
					yield return addr.Address.ToString();
			if(this._controlHost != null)
				foreach(ServiceEndpoint addr in this._controlHost.Description.Endpoints)
					yield return addr.Address.ToString();
			if(this._controlProxy != null)
				foreach(ServiceEndpoint addr in this._controlProxy.PluginsHost.Description.Endpoints)
					yield return addr.Address.ToString();
		}

		private Boolean TryCreateWebHost()
		{
			Boolean isSuccess = false;
			try
			{
				if((this._serviceType & PluginSettings.ServiceType.REST) == PluginSettings.ServiceType.REST)
				{
					ServiceHost host = ServiceConfiguration.Instance.CreateWeb<PluginsService, IPluginsService>(this._hostUrl);

					host.Open();
					host.Faulted += ControlHost_Faulted;
					this._restHost = host;
					isSuccess = true;
				}
				if((this._serviceType & PluginSettings.ServiceType.SOAP) == PluginSettings.ServiceType.SOAP)
				{
					ServiceHost host = ServiceConfiguration.Instance.CreateSoap<PluginsService, IPluginsService>(this._hostUrl.TrimEnd('/') + "/soap");

					host.Open();
					host.Faulted += ControlHost_Faulted;
					this._soapHost = host;
					isSuccess = true;
				}
			} catch(AddressAlreadyInUseException)
			{
				// Address is in use by different process
			} catch(AddressAccessDeniedException exc)
			{
				CheckAdministratorAccess(this._hostUrl, exc);
				throw;
			}
			return isSuccess;
		}

		private void TryCreateControlProxy()
		{
			try
			{
				/*TODO: There is an intermittent exception when _controlWebHost is already open but _controlHost is not yet created.
				In this case _controlProxy cannot connect to the not yet created _controlHost*/
				this._controlProxy = new ControlServiceProxy(this.BaseControlAddress, "Host");
				this._controlProxy.Open();
				this._controlProxy.CreateClientHost();
			} catch(System.ServiceModel.EndpointNotFoundException exc)
			{
				exc.Data.Add("Host", this._hostUrl);
				exc.Data.Add("IpcHost", this.BaseControlAddress + "/Host");
				Plugin.Trace.TraceEvent(TraceEventType.Warning, 6, String.Format("IPC control host not found. Probably address {0} already in use in different application", this._hostUrl));
				throw;
			}
		}

		public void Connect(String hostUrl, PluginSettings.ServiceType serviceType)
		{
			if(String.IsNullOrEmpty(hostUrl))
				throw new ArgumentNullException(nameof(hostUrl));

			this._hostUrl = hostUrl;
			this._serviceType = serviceType;
			this._ipc = new IpcSingleton("Global\\Plugin.WcfServer." + this._hostUrl.GetHashCode().ToString(), new TimeSpan(0, 0, 10));
			this._ipc.Mutex<Object>(null, p =>
			{
				try
				{
					if(this._serviceType != PluginSettings.ServiceType.None && this.TryCreateWebHost())
					{
						this._controlHost = ServiceConfiguration.Instance.Create<ControlService, IControlService>(this.BaseControlAddress, "Host");
						this._controlHost.Open();
						this._controlHost.Faulted += ControlHost_Faulted;
					} else
					{
						this.TryCreateControlProxy();
					}
					this._ping = new Timer(this.TimerCallback, this, 5000, 5000);

					this.Connected?.Invoke(this, EventArgs.Empty);
				} catch
				{
					this.Dispose();
					throw;
				}
			});
		}

		/// <summary>Check for administrator rights for current user and modify exception for fixing details</summary>
		private static void CheckAdministratorAccess(String serverUrl, Exception exc)
		{
			Boolean isAdministrator;
			using(WindowsIdentity identity = WindowsIdentity.GetCurrent())
				isAdministrator = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);

			if(!isAdministrator)
			{
				String hostUrl = serverUrl.EndsWith("/") ? serverUrl : (serverUrl + "/");
				exc.Data.Add("netsh", $"netsh http add urlacl url={hostUrl} user={Environment.UserDomainName}\\{Environment.UserName}");
				Plugin.Trace.TraceEvent(TraceEventType.Warning, 6, "You have to reserve host with netsh (see exception details for example) command or run application in [Administrator] mode.");
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(Boolean disposing)
		{
			if(disposing)
			{
				Stopwatch sw = new Stopwatch();
				sw.Start();

				CommunicationState state = this.State;
				lock(ObjLock)
				{
					if(this._ping != null)
					{
						this._ping.Dispose();
						this._ping = null;
					}

					AbortServiceHost(nameof(this._restHost), this._restHost, p => p.Abort());

					AbortServiceHost(nameof(this._soapHost), this._soapHost, p => p.Abort());

					AbortServiceHost(nameof(this._controlProxy), this._controlProxy, p => p.DisconnectControlHost());

					AbortServiceHost(nameof(this._controlHost), this._controlHost, p => p.Abort());
				}

				sw.Stop();
				Plugin.Trace.TraceEvent(TraceEventType.Verbose, 7, "Destroyed. State: {0} Elapsed: {1} ", state, sw.Elapsed);
			}
		}

		private static void AbortServiceHost<T>(String name, T service, Action<T> method) where T : class
		{
			if(service != null)
				try
				{
					method(service);
				} catch(CommunicationObjectFaultedException exc)
				{
					Plugin.Trace.TraceEvent(TraceEventType.Warning, 5, name + " Dispose exception: " + exc.Message);
				}
		}

		private static void ControlHost_Faulted(Object sender, EventArgs e)
			=> Plugin.Trace.TraceEvent(TraceEventType.Error, 10, "ControlHost is in faulted state");

		private void TimerCallback(Object state)
		{
			this._ping.Change(Timeout.Infinite, Timeout.Infinite);

			ServiceFactory communication = (ServiceFactory)state;
			try
			{
				if(communication.IsHost)
				{
					List<Int32> failedProxies = new List<Int32>();
					foreach(KeyValuePair<Int32, PluginsServiceProxy> proxy in ServiceFactory.Proxies)//TODO: In threads items may be added or removed from the dictionary
						try
						{
							_ = proxy.Value.Plugins.GetPlugins();
						} catch(System.ServiceModel.CommunicationObjectFaultedException exc)
						{
							Exception ei = exc.InnerException ?? exc;
							ei.Data.Add("ProxyId", proxy.Key);
							Plugin.Trace.TraceData(TraceEventType.Error, 10, ei);
							failedProxies.Add(proxy.Key);
						} /*catch(ProtocolException exc)
						{// There was a mistake like that once. Let's assume it was a fright.
							Exception ei = exc.InnerException == null ? exc : exc.InnerException;
							ei.Data.Add("ProxyId", proxy.Key);
							Plugin.Trace.TraceData(TraceEventType.Error, 10, ei);
							failedProxies.Add(proxy.Key);
						}*/

					foreach(Int32 id in failedProxies)//Remove proxies that failed to connect (TODO: Possibly give multiple attempts to reconnect)
						ServiceFactory.Proxies.Remove(id);

				} else if(!this._controlProxy.Ping())
				{
					Plugin.Trace.TraceEvent(TraceEventType.Verbose, 7, "Control Proxy PING failed. Reconnecting...");
					this.Dispose();
					this.Connect(this._hostUrl, this._serviceType);
				}
			} catch(Exception exc)
			{
				Exception ei = exc.InnerException ?? exc;
				Plugin.Trace.TraceData(TraceEventType.Error, 10, ei);
			} finally
			{
				this._ping?.Change(5000, 5000);
			}
		}
	}
}