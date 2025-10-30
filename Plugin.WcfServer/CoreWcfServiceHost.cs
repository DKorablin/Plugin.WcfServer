#if !NET35
using System;
using System.Threading;
using System.Threading.Tasks;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Plugin.WcfServer
{
	/// <summary>
	/// Wrapper for CoreWCF service hosting in .NET 8
	/// Provides a similar interface to System.ServiceModel.ServiceHost
	/// </summary>
	public class CoreWcfServiceHost : IDisposable
	{
		private IHost _host;
		private readonly Type _serviceType;
		private readonly Uri _baseAddress;
		private CommunicationState _state = CommunicationState.Created;
		private readonly Object _syncLock = new Object();

		public event EventHandler Faulted;
		
		public CommunicationState State
		{
			get { lock (_syncLock) return _state; }
			private set { lock (_syncLock) _state = value; }
		}

		public ServiceDescription Description { get; private set; }

		public CoreWcfServiceHost(Type serviceType, params Uri[] baseAddresses)
		{
			_serviceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
			_baseAddress = baseAddresses != null && baseAddresses.Length > 0 ? baseAddresses[0] : null;
			
			// Create a minimal ServiceDescription for compatibility
			Description = new ServiceDescription();
		}

		public CoreWcfServiceHost(Object singletonInstance, params Uri[] baseAddresses)
		{
			if (singletonInstance == null)
				throw new ArgumentNullException(nameof(singletonInstance));
				
			_serviceType = singletonInstance.GetType();
			_baseAddress = baseAddresses != null && baseAddresses.Length > 0 ? baseAddresses[0] : null;
			
			Description = new ServiceDescription();
		}

		public ServiceEndpoint AddServiceEndpoint(Type implementedContract, CoreWCF.Channels.Binding binding, String address)
		{
			// Store endpoint information for later use
			var endpoint = new ServiceEndpoint(new ContractDescription(implementedContract.Name));
			endpoint.Address = new EndpointAddress(new Uri(_baseAddress, address));
			endpoint.Binding = binding;
			
			Description.Endpoints.Add(endpoint);
			return endpoint;
		}

		public void Open()
		{
			if (State != CommunicationState.Created && State != CommunicationState.Closed)
				throw new InvalidOperationException($"Cannot open service in state: {State}");

			State = CommunicationState.Opening;

			try
			{
				// For named pipe hosting, we use a minimal host
				// CoreWCF requires an IHost but we'll create a lightweight one
				var hostBuilder = Host.CreateDefaultBuilder()
					.ConfigureServices((context, services) =>
					{
						services.AddServiceModelServices();
						
						// Add the service
						if (_serviceType != null)
						{
							services.AddSingleton(_serviceType);
						}
					})
					.ConfigureServices(services =>
					{
						// Configure CoreWCF
						services.AddServiceModelServices();
					});

				_host = hostBuilder.Build();
				_host.StartAsync().Wait();
				
				State = CommunicationState.Opened;
			}
			catch (Exception)
			{
				State = CommunicationState.Faulted;
				Faulted?.Invoke(this, EventArgs.Empty);
				throw;
			}
		}

		public void Close()
		{
			Close(TimeSpan.FromSeconds(10));
		}

		public void Close(TimeSpan timeout)
		{
			if (State == CommunicationState.Closed || State == CommunicationState.Closing)
				return;

			State = CommunicationState.Closing;

			try
			{
				if (_host != null)
				{
					using (var cts = new CancellationTokenSource(timeout))
					{
						_host.StopAsync(cts.Token).Wait();
					}
				}
				State = CommunicationState.Closed;
			}
			catch
			{
				State = CommunicationState.Faulted;
				throw;
			}
		}

		public void Abort()
		{
			State = CommunicationState.Closing;
			try
			{
				_host?.StopAsync(TimeSpan.FromSeconds(1)).Wait();
			}
			catch
			{
				// Ignore exceptions during abort
			}
			finally
			{
				State = CommunicationState.Closed;
			}
		}

		public void Dispose()
		{
			if (State == CommunicationState.Opened)
			{
				try
				{
					Close();
				}
				catch
				{
					Abort();
				}
			}

			_host?.Dispose();
			_host = null;
		}
	}
}
#endif
