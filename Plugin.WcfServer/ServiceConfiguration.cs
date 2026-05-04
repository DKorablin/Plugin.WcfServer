using System;
using System.Configuration;
#if NETFRAMEWORK
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Web.Configuration;
using System.Web.Hosting;
#else
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using CoreWCF.Channels;
using CoreWCF.Web;
using ServiceHost = Plugin.WcfServer.CoreWcfServiceHost;
#endif

namespace Plugin.WcfServer
{
	internal sealed class ServiceConfiguration
	{
#if NETFRAMEWORK
		private readonly ServiceModelSectionGroup _serviceModelGroup;
#endif

		public static readonly ServiceConfiguration Instance = new ServiceConfiguration();

		private ServiceConfiguration()
		{
#if NETFRAMEWORK
			Configuration configuration = HostingEnvironment.IsHosted
				? WebConfigurationManager.OpenWebConfiguration("~")
				: ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

			this._serviceModelGroup = configuration == null ? null : ServiceModelSectionGroup.GetSectionGroup(configuration);
#endif
		}

		public ServiceHost Create<TService, TEndpoint>(String baseAddress, String address)
		{
#if NETFRAMEWORK
			if(this.CheckServiceConfiguration<TEndpoint>())
				return new ServiceHost(typeof(TService));
			else
			{
				ServiceHost result = new ServiceHost(typeof(TService), new Uri(baseAddress));

				NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
				{//https://stackoverflow.com/questions/2911221/what-is-the-purpose-of-wcf-reliable-session
					ReceiveTimeout = TimeSpan.MaxValue,
				};
				ServiceEndpoint endpoint = result.AddServiceEndpoint(typeof(TEndpoint), binding, address);

				return result;
			}
#else
			ServiceHost result = new ServiceHost(typeof(TService), new Uri(baseAddress));

			NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
			{
				ReceiveTimeout = TimeSpan.MaxValue,
			};
			ServiceEndpoint endpoint = result.AddServiceEndpoint(typeof(TEndpoint), binding, address);

			return result;
#endif
		}

		public ServiceHost CreateWeb<TService, TEndpoint>(String baseAddress)
		{
#if NETFRAMEWORK
			if(this.CheckServiceConfiguration<TEndpoint>())
				return new ServiceHost(typeof(TService));
			else
			{
				WebHttpBinding binding = new WebHttpBinding()
				{
				};
				WebHttpBehavior behavior = new WebHttpBehavior()
				{
					DefaultOutgoingResponseFormat = WebMessageFormat.Json,
					DefaultOutgoingRequestFormat = WebMessageFormat.Json,
				};

				ServiceHost result = new WebServiceHost(typeof(TService), new Uri(baseAddress));
				ServiceEndpoint endpoint = result.AddServiceEndpoint(typeof(TEndpoint), binding, String.Empty);
				endpoint.Behaviors.Add(behavior);

				return result;
			}
#else
			WebHttpBinding binding = new WebHttpBinding()
			{
			};

			ServiceHost result = new ServiceHost(typeof(TService), new Uri(baseAddress));
			ServiceEndpoint endpoint = result.AddServiceEndpoint(typeof(TEndpoint), binding, String.Empty);

			// Note: CoreWCF WebHttpBehavior requires IServiceProvider which is not available in this context
			// The behavior will need to be configured differently or the endpoint used as-is

			return result;
#endif
		}

		public ServiceHost CreateSoap<TService,TEndpoint>(String baseAddress)
		{
#if NETFRAMEWORK
			if(this.CheckClientConfiguration<TEndpoint>())
				return new ServiceHost(typeof(TService));
			else
			{
				ServiceHost result = new ServiceHost(typeof(TService), new Uri(baseAddress));

				WSHttpBinding binding = new WSHttpBinding()
				{
				};
				ServiceMetadataBehavior behavior = new ServiceMetadataBehavior()
				{
					HttpGetEnabled = true,
				};
				behavior.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
				result.Description.Behaviors.Add(behavior);
				result.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
				result.AddServiceEndpoint(typeof(TEndpoint), binding, String.Empty);

				return result;
			}
#else
			ServiceHost result = new ServiceHost(typeof(TService), new Uri(baseAddress));

			WSHttpBinding binding = new WSHttpBinding()
			{
			};
			ServiceMetadataBehavior behavior = new ServiceMetadataBehavior()
			{
				HttpGetEnabled = true,
			};
			result.Description.Behaviors.Add(behavior);

			// Note: CoreWCF metadata exchange may need different configuration
			// result.AddServiceEndpoint for metadata exchange

			result.AddServiceEndpoint(typeof(TEndpoint), binding, String.Empty);

			return result;
#endif
		}

#if NETFRAMEWORK
		private Boolean CheckClientConfiguration<TEndpoint>()
		{
			if(this._serviceModelGroup == null)
				return false;

			foreach(ChannelEndpointElement endpoint in this._serviceModelGroup.Client.Endpoints)
				if(endpoint.Contract == typeof(TEndpoint).FullName)
					return true;

			return false;
		}

		private Boolean CheckServiceConfiguration<TService>()
			=> this.CheckServiceConfiguration(typeof(TService));

		private Boolean CheckServiceConfiguration(Type serviceType)
		{
			/*
			TODO: To ensure reading from the .config file works correctly, you need to create two different contracts.
			One for the Web, and the other for the IPC. Separating the interfaces doesn't work.
			*/
			if(this._serviceModelGroup == null)
				return false;

			String serviceTypeName = serviceType.FullName;
			foreach(ServiceElement service in this._serviceModelGroup.Services.Services)
				if(service.Name == serviceTypeName)
					return true;

			return false;
		}
#endif
	}
}