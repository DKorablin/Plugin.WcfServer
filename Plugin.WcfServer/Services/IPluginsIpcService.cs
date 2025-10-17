using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using Plugin.WcfServer.Data;

namespace Plugin.WcfServer.Services
{
	/// <summary>Service for IPC interaction</summary>
	[ServiceContract]
	public interface IPluginsIpcService
	{
		/// <summary>Get a list of all plugins loaded into the current host</summary>
		/// <returns>List of all plugins loaded into the current host</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "Plugins", BodyStyle = WebMessageBodyStyle.Bare)]
		PluginData[] GetPlugins();

		/// <summary>Get plugin information including all its members</summary>
		/// <param name="id">Plugin identifier</param>
		/// <returns>Plugin information in JSON format</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "Plugins/{id}", BodyStyle = WebMessageBodyStyle.Bare)]
		String GetPlugin(String id);

		/// <summary>Invoke a plugin property or method that does not expect input arguments</summary>
		/// <param name="id">Plugin identifier</param>
		/// <param name="memberName">Name of the property or method that does not expect input parameters</param>
		/// <returns>Response from the plugin in JSON format</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "Plugins/{id}/{*memberName}", BodyStyle = WebMessageBodyStyle.Bare)]
		String InvokeGetMember(String id, String memberName);

		/// <summary>Invoke a plugin method passing JSON data in the HTTP request body as method arguments</summary>
		/// <param name="id">Plugin identifier</param>
		/// <param name="memberName">Name of the method in the plugin</param>
		/// <returns>Response from the plugin in JSON format</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "POST", UriTemplate = "Plugins/{id}/{memberName}/{*payload}", BodyStyle = WebMessageBodyStyle.Bare)]
		String InvokeMember(String id, String memberName, String payload);
	}
}