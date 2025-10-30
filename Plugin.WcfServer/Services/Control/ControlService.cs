﻿using System;
using System.Diagnostics;
using System.Net;
#if NET35
using System.ServiceModel;
#else
using CoreWCF;
#endif

namespace Plugin.WcfServer.Services.Control
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
	public class ControlService : IControlService
	{
		private readonly Int32 _processId = Process.GetCurrentProcess().Id;

		/// <summary>Connect process to notification system.</summary>
		/// <param name="processId">Process ID to connect.</param>
		/// <param name="endpointAddress">IPC endpoint address of process.</param>
		public Int32 Connect(Int32 processId, String endpointAddress)
		{
			if(ServiceFactory.Proxies.ContainsKey(processId))
				throw new FaultException($"Connect -> ControlServiceProxy ({processId:N0}) already registered", new FaultCode(HttpStatusCode.BadRequest.ToString()));

			PluginsServiceProxy proxy = new PluginsServiceProxy(endpointAddress);
			ServiceFactory.Proxies.Add(processId, proxy);

			Plugin.Trace.TraceEvent(TraceEventType.Information, 5, "ControlHost ({0:N0}): ControlServiceProxy ({1:N0}) connected. Address: {2} Total: {3:N0}", this._processId, processId, endpointAddress, ServiceFactory.Proxies.Count);
			return this._processId;
		}

		/// <summary>Disconnect process from notification system.</summary>
		/// <param name="processId">Process ID to disconnect.</param>
		public void Disconnect(Int32 processId)
		{
			if(!ServiceFactory.Proxies.Remove(processId))
				throw new FaultException($"Disconnect -> ControlServiceProxy ({processId:N0}) not registered", new FaultCode(HttpStatusCode.BadRequest.ToString()));

			Plugin.Trace.TraceEvent(TraceEventType.Information, 5, "ControlHost ({0:N0}): ControlServiceProxy ({1:N0}) disconnected. Total: {2:N0}", this._processId, processId, ServiceFactory.Proxies.Count);
			/*foreach(PluginsServiceProxy item in this.Proxies.Values)
				item.ClientMethod(String.Format("ProcessId: {0:N0} disconnected", processId));*/
		}

		public Int32 Ping(Int32 processId)
			=> ServiceFactory.Proxies.ContainsKey(processId)
				? this._processId
				: throw new FaultException($"Ping -> ControlServiceProxy {processId:N0} not registered", new FaultCode(HttpStatusCode.BadRequest.ToString()));
	}
}