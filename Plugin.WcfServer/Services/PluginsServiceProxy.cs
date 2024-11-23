using System;
using System.ServiceModel;

namespace Plugin.WcfServer.Services
{
	public class PluginsServiceProxy : ClientBase<IPluginsIpcService>
	{
		public IPluginsIpcService Plugins => base.Channel;

		public PluginsServiceProxy(String address)
			:base(new NetNamedPipeBinding(NetNamedPipeSecurityMode.None), new EndpointAddress(address))
		{ }
	}
}