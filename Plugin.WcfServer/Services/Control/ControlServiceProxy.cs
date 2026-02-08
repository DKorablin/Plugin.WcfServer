using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
#if NETFRAMEWORK
using ServiceHost = System.ServiceModel.ServiceHost;
#else
using CommunicationState = System.ServiceModel.CommunicationState;
using ServiceHost = Plugin.WcfServer.CoreWcfServiceHost;
#endif

namespace Plugin.WcfServer.Services.Control
{
	internal class ControlServiceProxy : ClientBase<IControlService>
	{
		private readonly Int32 _processId = Process.GetCurrentProcess().Id;
		private readonly String _baseHostAddress;
		private readonly String _relativeAddress;

		public String ClientBaseAddress => this._baseHostAddress + "/" + this._processId;

		public String ClientAddress => this.ClientBaseAddress + "/Plugins";

		public ServiceHost PluginsHost { get; private set; }

		public ControlServiceProxy(String baseAddress, String address)
			: base(new System.ServiceModel.NetNamedPipeBinding(System.ServiceModel.NetNamedPipeSecurityMode.None),
				new System.ServiceModel.EndpointAddress(baseAddress + "/" + address))
		{
			this._baseHostAddress = baseAddress;
			this._relativeAddress = address;
		}

		public void CreateClientHost()
		{
			this.PluginsHost = ServiceConfiguration.Instance.Create<PluginsIpcService, IPluginsIpcService>(this.ClientBaseAddress, "Plugins");
			this.PluginsHost.Faulted += NotifyHost_Faulted;
			this.PluginsHost.Open();
			Int32 hostProcessId = base.Channel.Connect(this._processId, this.ClientAddress);
			Plugin.Trace.TraceEvent(TraceEventType.Start, 11, "ControlServiceProxy ({0:N0}): Connected to ControlHost ({1:N0})", this._processId, hostProcessId);
		}

		public Boolean Ping()
		{
			switch(this.State)
			{
			case CommunicationState.Opened:
				try
				{
					Int32 hostProcessId = base.Channel.Ping(this._processId);
					//Console.WriteLine("ClientHost Ping: HostProcessId: {0}", hostProcessId);
				} catch(System.ServiceModel.FaultException exc)
				{
					Console.WriteLine(exc.Message);
				} catch(System.ServiceModel.CommunicationException exc)
				{
					PipeException pipeExc = (PipeException)exc.InnerException;
					if(pipeExc != null)
					{
						switch(pipeExc.ErrorCode)
						{
						case 232:
							return false;
						}
					} else
						throw;
				}
				return true;
			case CommunicationState.Faulted:
			default:
				return false;
			}
		}

		private void NotifyHost_Faulted(Object sender, EventArgs e)
			=> Plugin.Trace.TraceEvent(TraceEventType.Warning, 7, "ControlServiceProxy ({0:N0}): Faulted state", this._processId);

		public void DisconnectControlHost()
		{
			if(base.State == CommunicationState.Opened)
				try
				{
					base.Channel.Disconnect(this._processId);
					base.Close();
				} catch(System.ServiceModel.CommunicationException exc)
				{//Error when trying to disconnect from process. In theory check for control process existence may be needed
					Plugin.Trace.TraceEvent(TraceEventType.Warning, 7, "ControlServiceProxy ({0:N0}): Dispose exception. Message: {1}", this._processId, exc.Message);
				}

			if(this.PluginsHost != null && this.PluginsHost.State == CommunicationState.Opened)
			{
				this.PluginsHost.Abort();
				this.PluginsHost = null;
			}
		}
	}
}