using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
#if NET35
using System.ServiceModel;
using System.Web;
#else
using CoreWCF;
using System.Web;
#endif
using Plugin.WcfServer.Data;
using SAL.Flatbed;

namespace Plugin.WcfServer.Services
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
	public class PluginsIpcService : IPluginsIpcService
	{
		internal Plugin SPlugin => Plugin.SPlugin;

		public Data.PluginData[] GetPlugins()
			=> this.SPlugin.Host.Plugins.Select(p => new Data.PluginData(p)).ToArray();

		String IPluginsIpcService.GetPlugin(String id)
		{
			IPluginDescription plugin = this.GetPluginById(id);
			if(plugin == null)
				return null;

			TypeInfoData result = new TypeInfoData(plugin, plugin.Type);
			return Serializer.JavaScriptSerialize(result);//HACK: Does not return results when TypeInfoData.Members is populated
		}

		String IPluginsIpcService.InvokeGetMember(String id, String memberName)
		{
			Int32 index = memberName == null ? -1 : memberName.IndexOf('?');
			String payload = null;
			if(index > -1)
			{
				payload = memberName.Substring(index);
				memberName = memberName.Substring(0, index);
			}

			return this.InvokeMember(id, memberName, payload);
			
		}

		public String InvokeMember(String id, String memberName, String payload)
		{
			if(String.IsNullOrEmpty(memberName))
				throw new FaultException<String>(nameof(memberName), new FaultReason("memberName not specified"), new FaultCode(HttpStatusCode.BadRequest.ToString()));

			IPluginDescription plugin = this.GetPluginById(id)
				?? throw new FaultException<String>($"Plugin with ID={id} not found", new FaultReason("Plugin not found"), new FaultCode(HttpStatusCode.NotFound.ToString()));

			try
			{
				foreach(IPluginMemberInfo member in plugin.Type.Members)
					if(member.Name == memberName)
					{
						if(member is IPluginPropertyInfo pMember)
						{
							Object result = pMember.Get();
							return Serializer.JavaScriptSerialize(result);
						} else if(member is IPluginMethodInfo pMethod)
						{
							IPluginMethodInfo method = pMethod;
							Object[] parameters = new Object[method.Count];
							Int32 count = 0;
							if(method.Count > 0)
							{
								/*if(String.IsNullOrEmpty(payload))//https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/uritemplate-and-uritemplatetable
									payload = Encoding.UTF8.GetString(OperationContext.Current.RequestContext.RequestMessage.GetBody<Byte[]>()); - Doesn't work if proxied via RPC*/
								if(payload != null && payload.StartsWith("?"))
								{
									var keyValue = HttpUtility.ParseQueryString(payload);
									foreach(IPluginParameterInfo arg in method.GetParameters())
									{
										String value = keyValue.Get(arg.Name);
										if(value == null)
										{//Argument not found, looking for the next method
											count = -1;
											break;
											//parameters[count] = null;
										} else if(arg.TypeName == typeof(String).FullName)
											parameters[count] = value;
										else if(arg.IsArray)
										{
											parameters[count] = value.Split(',')
												.Select(p => p == null || p.Length == 0 ? null : TypeDescriptor.GetConverter(Plugin.GetType(arg.TypeName))//TODO: There will be an error here, because this is an Array and GetType will return []
													.ConvertFromString(p))
													.ToArray();
										} else
											parameters[count] = TypeDescriptor.GetConverter(Plugin.GetType(arg.TypeName)).ConvertFromString(value);
										count++;
									}
								} else
								{
									Dictionary<String, Object> keyValue = Serializer.JavaScriptDeserialize(payload);
									foreach(IPluginParameterInfo arg in method.GetParameters())
									{
										Object value = keyValue.TryGetValue(arg.Name, out value) ? value : null;

										if(value == null)
										{//Argument not found, looking for the next method
											count = -1;
											break;
											//parameters[count] = null;
										} else if(arg.TypeName == value.GetType().FullName)
											parameters[count] = value;
										else if(arg.IsArray)//TODO: Arrays need to be deserialized differently...
											parameters[count] = Serializer.JavaScriptDeserialize(Plugin.GetType(arg.TypeName), Serializer.JavaScriptSerialize(value));
										else
											parameters[count] = TypeDescriptor.GetConverter(Plugin.GetType(arg.TypeName)).ConvertFrom(value);// Serializer.JavaScriptDeserialize(Plugin.GetType(arg.TypeName), value);
										count++;
									}
								}
							}

							if(count != -1)
							{
								Object result = method.Invoke(parameters);
								return Serializer.JavaScriptSerialize(result);
							}
						}
					}
			} catch(TargetInvocationException exc)
			{
				Exception exc1 = exc.InnerException ?? exc;

				throw new FaultException<String>($"Plugin with ID={id} Member={memberName} throws {exc1.GetType()}", new FaultReason(exc1.Message), new FaultCode(HttpStatusCode.InternalServerError.ToString()));
			}

			throw new FaultException<String>(nameof(memberName), new FaultReason($"Member={memberName} in Plugin={id} not found"), new FaultCode(HttpStatusCode.NotFound.ToString()));
			// else throw new ArgumentException(String.Format("Member={0} in Plugin={1} uninvokable", memberName, id), "memberName");
		}

		internal IPluginDescription GetPluginById(String id)
		{
			if(String.IsNullOrEmpty(id))
				throw new FaultException<String>("Plugin ID not specified", new FaultReason("Missing PluginID"), new FaultCode(HttpStatusCode.BadRequest.ToString()));

			IPluginDescription plugin = this.SPlugin.Host.Plugins[id];
			return plugin;
		}
	}
}