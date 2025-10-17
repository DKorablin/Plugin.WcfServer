using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using Plugin.WcfServer.Data;

namespace Plugin.WcfServer.Services
{
	/// <summary>Service for plugin information and manipulation</summary>
	[ServiceContract]
	public interface IPluginsService : IPluginsIpcService
	{
		/// <summary>Get a list of instances where the same core is running</summary>
		/// <returns></returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "Instance", BodyStyle = WebMessageBodyStyle.Bare)]
		Int32[] GetInstance();

		/// <summary>Get a list of all plugins loaded into the current host</summary>
		/// <param name="instance">Get plugins in a specific instance</param>
		/// <returns>List of all plugins loaded into the current host</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "{instance}/Plugins", BodyStyle = WebMessageBodyStyle.Bare)]
		PluginData[] GetIpcPlugins(String instance);

		/// <summary>Get plugin information including all its members</summary>
		/// <param name="instance">Get plugins in a specific instance</param>
		/// <param name="id">Plugin identifier</param>
		/// <returns>Plugin information in JSON format</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "{instance}/Plugins/{id}", BodyStyle = WebMessageBodyStyle.Bare)]
		String GetIpcPlugin(String instance, String id);

		/// <summary>Invoke a plugin property or method that does not expect input arguments</summary>
		/// <param name="instance">Execute the method in a specific instance</param>
		/// <param name="id">Plugin identifier</param>
		/// <param name="memberName">Name of the property or method that does not expect input parameters</param>
		/// <returns>Response from the plugin in JSON format</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "{instance}/Plugins/{id}/{memberName}", BodyStyle = WebMessageBodyStyle.Bare)]
		String InvokeIpcGetMember(String instance, String id, String memberName);

		/// <summary>Invoke a plugin method passing JSON data in the HTTP request body as method arguments</summary>
		/// <param name="instance">Execute the method in a specific instance</param>
		/// <param name="id">Plugin identifier</param>
		/// <param name="memberName">Name of the property or method that does not expect input parameters</param>
		/// <returns>Response from the plugin in JSON format</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "POST", UriTemplate = "{instance}/Plugins/{id}/{memberName}", BodyStyle = WebMessageBodyStyle.Bare)]
		String InvokeIpcMember(String instance, String id, String memberName);

		/// <summary>Invoke a plugin method passing JSON data in the HTTP request body as method arguments</summary>
		/// <param name="id">Plugin identifier</param>
		/// <param name="memberName">Name of the property or method that does not expect input parameters</param>
		/// <returns>First found response from the plugin in JSON format</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "POST", UriTemplate = "Plugin/{id}/{memberName}", BodyStyle = WebMessageBodyStyle.Bare)]
		String FindAndInvokeIpcMember(String id, String memberName);
	}
}