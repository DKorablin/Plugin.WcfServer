﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;
using Plugin.WcfServer.Data;

namespace Plugin.WcfServer.Services
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
	public class PluginsService : IPluginsService
	{
		private PluginsIpcService _ipcService;
		private PluginsIpcService IpcService
			=> this._ipcService == null ? this._ipcService = new PluginsIpcService() : this._ipcService;

		#region Ipc
		PluginData[] IPluginsIpcService.GetPlugins()
			=> this.IpcService.GetPlugins();

		String IPluginsIpcService.GetPlugin(String id)
		{
			IPluginsIpcService service = this.IpcService;
			return service == null ? null : service.GetPlugin(id);
		}

		String IPluginsIpcService.InvokeGetMember(String id, String memberName)
		{//TODO: Подумать о необходиости этого метода, сделан исключительно для диагностики
			if(String.IsNullOrEmpty(memberName))
				throw new ArgumentNullException(nameof(memberName));

			IPluginsIpcService service = this.IpcService
				?? throw new FaultException<String>(String.Format("Plugin with ID={0} not found", id), new FaultReason("Plugin not found"), new FaultCode(HttpStatusCode.NotFound.ToString()));

			try
			{
				return service.InvokeGetMember(id, memberName);
			} catch(TargetInvocationException exc)
			{
				Exception exc1 = exc.InnerException == null
					? exc
					: exc.InnerException;

				throw new FaultException<String>(String.Format("Plugin with ID={0} Member={1} throws {2}", id, memberName, exc.GetType()), new FaultReason(exc1.Message), new FaultCode(HttpStatusCode.InternalServerError.ToString()));
			} catch(Exception exc)
			{
				if(Utils.IsFatal(exc))
					throw;

				Plugin.Trace.TraceData(TraceEventType.Error, 10, exc);
				throw new FaultException<String>(String.Format("Plugin with ID={0} Member={1} throws {2}", id, memberName, exc.GetType()), new FaultReason(exc.Message), new FaultCode(HttpStatusCode.InternalServerError.ToString()));
			}
		}

		public String InvokeMember(String id, String memberName, String payload)
		{
			if(String.IsNullOrEmpty(memberName))
				throw new ArgumentNullException(nameof(memberName));

			IPluginsIpcService service = this.IpcService
				?? throw new FaultException<String>(String.Format("Plugin with ID={0} not found", id), new FaultReason("Plugin not found"), new FaultCode(HttpStatusCode.NotFound.ToString()));

			try
			{
				return service.InvokeMember(id, memberName, payload);
			} catch(TargetInvocationException exc)
			{
				Exception exc1 = exc.InnerException == null
					? exc
					: exc.InnerException;

				throw new FaultException<String>(String.Format("Plugin with ID={0} Member={1} throws {2}", id, memberName, exc1.GetType()), new FaultReason(exc1.Message), new FaultCode(HttpStatusCode.InternalServerError.ToString()));
			} catch(Exception exc)
			{
				if(Utils.IsFatal(exc))
					throw;

				Plugin.Trace.TraceData(TraceEventType.Error, 10, exc);
				throw new FaultException<String>(String.Format("Plugin with ID={0} Member={1} throws {2}", id, memberName, exc.GetType()), new FaultReason(exc.Message), new FaultCode(HttpStatusCode.InternalServerError.ToString()));
			}
		}
		#endregion Ipc
		#region Rest
		Int32[] IPluginsService.GetInstance()
		{
			Int32[] instances = ServiceFactory.Proxies.Keys.ToArray();
			Array.Resize(ref instances, instances.Length + 1);
			instances[instances.Length - 1] = Process.GetCurrentProcess().Id;//Инстанс хоста
			return instances;
		}
		PluginData[] IPluginsService.GetIpcPlugins(String instance)
		{
			IPluginsIpcService service = this.GetInstance(instance);
			return service.GetPlugins();
		}

		String IPluginsService.GetIpcPlugin(String instance, String id)
		{
			IPluginsIpcService service = this.GetInstance(instance);
			return service.GetPlugin(id);
		}

		String IPluginsService.InvokeIpcGetMember(String instance, String id, String memberName)
		{
			IPluginsIpcService service = this.GetInstance(instance);
			return service.InvokeGetMember(id, memberName);
		}

		String IPluginsService.InvokeIpcMember(String instance, String id, String memberName)
		{
			IPluginsIpcService service = this.GetInstance(instance);

			String payload = this.GetPayload();
			return service.InvokeMember(id, memberName, payload);
		}

		String IPluginsService.FindAndInvokeIpcMember(String id, String memberName)
		{
			IPluginsIpcService service = this.FindPluginsInstance(id)
				?? throw new FaultException<String>(String.Format("Plugin with ID: {0} not registered", id), new FaultReason("Plugin not found"), new FaultCode(HttpStatusCode.NotFound.ToString()));

			String payload = this.GetPayload();
			return service.InvokeMember(id, memberName, payload);
		}
		#endregion Rest

		private IPluginsIpcService GetInstance(String instance)
		{
			if(!Int32.TryParse(instance, out Int32 instanceId))
				throw new FaultException<String>("{instance} template parameter is required", new FaultReason("Instance ID not specified"), new FaultCode(HttpStatusCode.BadRequest.ToString()));

			if(instanceId == Process.GetCurrentProcess().Id)//Host instance
				return this.IpcService;

			if(ServiceFactory.Proxies.TryGetValue(instanceId, out PluginsServiceProxy proxy))
				return proxy.Plugins;

			throw new FaultException<String>(String.Format("Instance with ID: {0} not registered", instanceId), new FaultReason("Instance not registered"), new FaultCode(HttpStatusCode.NotFound.ToString()));
		}

		/// <summary>Search for plugin in different instances (The first one found is returned)</summary>
		/// <param name="pluginId">Plugin identifier</param>
		/// <returns>First found instance where plugin by id is found</returns>
		private IPluginsIpcService FindPluginsInstance(String pluginId)
		{
			if(this.IpcService.GetPluginById(pluginId) != null)
				return this.IpcService;

			foreach(PluginsServiceProxy proxy in ServiceFactory.Proxies.Values)
				if(proxy.Plugins.GetPlugin(pluginId) != null)
					return proxy.Plugins;

			return null;
		}

		private String GetPayload()
		{
			WebBodyFormatMessageProperty bodyFormat = (WebBodyFormatMessageProperty)OperationContext.Current.IncomingMessageProperties[WebBodyFormatMessageProperty.Name];
			if(bodyFormat != null && bodyFormat.Format == WebContentFormat.Json)
			{
				var requestMessage = OperationContext.Current.RequestContext.RequestMessage;
				var messageDataProperty = requestMessage.GetType().GetProperty("MessageData", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				Object messageData = messageDataProperty.GetValue(requestMessage, null);
				PropertyInfo bufferProperty = messageData.GetType().GetProperty("Buffer");
				ArraySegment<Byte> buffer = (ArraySegment<Byte>)bufferProperty.GetValue(messageData, null);
				return Encoding.UTF8.GetString(buffer.Array, 0, buffer.Count);
			} else
				return Encoding.UTF8.GetString(OperationContext.Current.RequestContext.RequestMessage.GetBody<Byte[]>());
		}
	}
}