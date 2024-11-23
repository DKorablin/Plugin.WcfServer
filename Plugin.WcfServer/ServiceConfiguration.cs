using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Web.Configuration;
using System.Web.Hosting;

namespace Plugin.WcfServer
{
	public sealed class ServiceConfiguration
	{
		private readonly ServiceModelSectionGroup _serviceModelGroup;

		public static readonly ServiceConfiguration Instance = new ServiceConfiguration();

		private ServiceConfiguration()
		{
			Configuration configuration = HostingEnvironment.IsHosted
				? WebConfigurationManager.OpenWebConfiguration("~")
				: ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

			this._serviceModelGroup = configuration == null ? null : ServiceModelSectionGroup.GetSectionGroup(configuration);
		}

		public ServiceHost Create<TService, TEndpoint>(String baseAddress, String address)
		{
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
		}

		public ServiceHost CreateSingle<TEndpoint>(Object service, String baseAddress, String address)
		{
			if(this.CheckServiceConfiguration(service.GetType()))
				return new ServiceHost(service);
			else
			{
				ServiceHost result = new ServiceHost(service, new Uri(baseAddress));

				NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
				{//https://stackoverflow.com/questions/2911221/what-is-the-purpose-of-wcf-reliable-session
					ReceiveTimeout = TimeSpan.MaxValue,
				};
				ServiceEndpoint endpoint = result.AddServiceEndpoint(typeof(TEndpoint), binding, address);

				return result;
			}
		}

		public ServiceHost CreateWeb<TService, TEndpoint>(String baseAddress)
		{
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
		}

		public ServiceHost CreateSoap<TService,TEndpoint>(String baseAddress)
		{
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
		}

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
			TODO: Чтобы корректно работало чтение из .config файла, необходимо создавать 2 разных контракта.
			Первый для Web, а второй для Ipc. Разделение на интефейсы - не работает
			*/
			if(this._serviceModelGroup == null)
				return false;

			String serviceTypeName = serviceType.FullName;
			foreach(ServiceElement service in this._serviceModelGroup.Services.Services)
				if(service.Name == serviceTypeName)
					return true;

			return false;
		}
	}
}