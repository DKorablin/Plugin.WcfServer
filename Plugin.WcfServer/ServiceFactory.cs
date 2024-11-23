using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using Plugin.WcfServer.Services;
using Plugin.WcfServer.Services.Control;

namespace Plugin.WcfServer
{
	internal class ServiceFactory : IDisposable
	{
		//TODO: Перейти на ConcurrentDictionary
		public static Dictionary<Int32, PluginsServiceProxy> Proxies = new Dictionary<Int32, PluginsServiceProxy>();

		private IpcSingleton _ipc;
		private ServiceHost _controlHost;
		private ServiceHost _restHost;
		private ServiceHost _soapHost;
		private ControlServiceProxy _controlProxy;
		private String _hostUrl;
		private PluginSettings.ServiceType _serviceType;
		private Timer _ping;
		private Object ObjLock = new Object();

		/// <summary>Базовый адрес для IPC соединения</summary>
		/// <remarks>Для идентификатора используется внешний Url, ибо он является объединяющим составляющим</remarks>
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
						return CommunicationState.Faulted;//Если они оба не запущены, то возвращаем ошибку
					if(this._soapHost != null && this._controlHost.State != this._soapHost.State)
						return CommunicationState.Faulted;//Если они оба не запущены, то возвращаем ошибку

					return this._controlHost.State;
				} else if(this._controlProxy != null)
					return this._controlProxy.State;
				else return CommunicationState.Closed;
			}
		}

		/// <summary>Получить список адресов, под которыми запущенны хосты</summary>
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
			} catch(AddressAccessDeniedException exc)
			{
				CheckAdministratorAccess(this._hostUrl, exc);
				throw;
			}
			return isSuccess;
		}

		private Boolean TryCreateControlProxy()
		{
			try
			{
				/*TODO: Тут бывает плавающее исключение, когда _controlWebHost уже открыт, а _controlHost ещё не создан.
				При этом, _controlProxy не может подключиться к ещё не созданному _controlHost'у*/
				this._controlProxy = new ControlServiceProxy(this.BaseControlAddress, "Host");
				this._controlProxy.Open();
				this._controlProxy.CreateClientHost();
				return true;
			} catch(EndpointNotFoundException exc)
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
					if(this._serviceType!=PluginSettings.ServiceType.None && this.TryCreateWebHost())
					{
						this._controlHost = ServiceConfiguration.Instance.Create<ControlService, IControlService>(this.BaseControlAddress, "Host");
						this._controlHost.Open();
						this._controlHost.Faulted += ControlHost_Faulted;
					} else
					{
						this.TryCreateControlProxy();
					}
					this._ping = new Timer(TimerCallback, this, 5000, 5000);

					this.Connected?.Invoke(this, EventArgs.Empty);
				} catch(Exception)
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

				AbortServiceHost(nameof(_restHost), this._restHost, p => p.Abort());

				AbortServiceHost(nameof(_soapHost), this._soapHost, p => p.Abort());

				AbortServiceHost(nameof(_controlProxy), this._controlProxy, p => p.DisconnectControlHost());

				AbortServiceHost(nameof(_controlHost), this._controlHost, p => p.Abort());
			}

			sw.Stop();
			Plugin.Trace.TraceEvent(TraceEventType.Verbose, 7, "Destroyed. State: {0} Elapsed: {1} ", state, sw.Elapsed);
		}

		private static void AbortServiceHost<T>(String name, T service, Action<T> method) where T : class
		{
			if(service != null)
				try
				{
					method(service);
					service = null;
				} catch(CommunicationObjectFaultedException exc)
				{
					Plugin.Trace.TraceEvent(TraceEventType.Warning, 5, name + " Dispose exception: " + exc.Message);
				}
		}

		private void ControlHost_Faulted(Object sender, EventArgs e)
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
					foreach(KeyValuePair<Int32, PluginsServiceProxy> proxy in ServiceFactory.Proxies)//TODO: В потоках может из словарика удаляться или добавляться объекты
						try
						{
							Data.PluginData[] data = proxy.Value.Plugins.GetPlugins();
						} catch(CommunicationObjectFaultedException exc)
						{
							Exception ei = exc.InnerException == null ? exc : exc.InnerException;
							ei.Data.Add("ProxyId", proxy.Key);
							Plugin.Trace.TraceData(TraceEventType.Error, 10, ei);
							failedProxies.Add(proxy.Key);
						} /*catch(ProtocolException exc)
						{// Один раз была такая ошибка. Будем считать что испугалося
							Exception ei = exc.InnerException == null ? exc : exc.InnerException;
							ei.Data.Add("ProxyId", proxy.Key);
							Plugin.Trace.TraceData(TraceEventType.Error, 10, ei);
							failedProxies.Add(proxy.Key);
						}*/

					foreach(Int32 id in failedProxies)//Удаляю прокси с которыми не удалось связаться (TODO: Возможно, стоит дать несколько попыток соедениться)
						ServiceFactory.Proxies.Remove(id);

				} else if(!this._controlProxy.Ping())
				{
					Plugin.Trace.TraceEvent(TraceEventType.Verbose, 7, "Control Proxy PING failed. Reconnecting...");
					this.Dispose();
					this.Connect(this._hostUrl, this._serviceType);
				}
			} catch(Exception exc)
			{
				Exception ei = exc.InnerException == null ? exc : exc.InnerException;
				Plugin.Trace.TraceData(TraceEventType.Error, 10, ei);
			} finally
			{
				this._ping?.Change(5000, 5000);
			}
		}
	}
}