using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Plugin.WcfServer.UI;
using SAL.Flatbed;

namespace Plugin.WcfServer
{
	public class PluginSettings : INotifyPropertyChanged
	{
		/// <summary>Тип создаваемого сервиса</summary>
		[Flags]
		public enum ServiceType
		{
			/// <summary>Не поднимать никакой сервис (Не знаю пока где такое может пригодиться)</summary>
			None = 1 << 0,
			/// <summary>Поднять сервис работающий по REST протоколу</summary>
			REST = 1 << 1,
			/// <summary>Поднять сервис работающий по SOAP протоколу</summary>
			SOAP = 1 << 2,
		}

		private static class Constants {
			public const String TemplateIpAddr = "{ipAddress}";
			public const String SWebHost = "http://" + TemplateIpAddr + ":8080";
			public const String SSoapHost = SWebHost + "/soap";
		}

		private readonly IHost _host;
		private String _hostUrl = Constants.SWebHost;
		private ServiceType _serviceType = ServiceType.REST;
		private static IPAddress _HostAddress;

		[Category("Server")]
		[DefaultValue(Constants.SWebHost)]
		[Description("Deployment REST/SOAP host for WCF service. Use " + Constants.TemplateIpAddr + " template for Dns.GetHostName()")]
		public String HostUrl
		{
			get => this._hostUrl;
			set => this.SetField(ref this._hostUrl,
				String.IsNullOrEmpty(value) ? Constants.SWebHost : value,
				nameof(this.HostUrl));
		}

		[Category("Server")]
		[DisplayName("Service Type")]
		[DefaultValue(ServiceType.REST)]
		[Description("Protocol types that will be used for communicaion")]
		[Editor(typeof(ColumnEditor<ServiceType>), typeof(UITypeEditor))]
		public ServiceType Type
		{
			get => this._serviceType;
			set => this.SetField(ref this._serviceType,
				value < 0 ? ServiceType.REST : value,
				nameof(this.Type));
		}

		/// <summary>Хост адрес текущей машины</summary>
		private static IPAddress HostAddress
		{
			get
			{
				if(_HostAddress == null)
				{
					IPHostEntry ip = Dns.GetHostEntry(Dns.GetHostName());
					PluginSettings._HostAddress = Array.Find(ip.AddressList, addr => addr.AddressFamily == AddressFamily.InterNetwork);
				}
				return _HostAddress;
			}
		}

		internal PluginSettings(IHost host)
			=> this._host = host;

		/// <summary>Получить наименование приложения для которого прописывается функция автозапуска</summary>
		internal String GetApplicationName()
		{
			StringBuilder result = new StringBuilder();
			foreach(IPluginDescription kernel in this._host.Plugins.FindPluginType<IPluginKernel>())
				result.Append(kernel.ID);

			return result.ToString();
		}

		/// <summary>Получить ност с кастомным форматированием</summary>
		/// <returns>Хост с дополнительным форматированием</returns>
		internal String GetHostUrl()
		{
			String result = this.HostUrl;

			return result.Contains(Constants.TemplateIpAddr)
				? result.Replace(Constants.TemplateIpAddr, PluginSettings.HostAddress.ToString())
				: result;
		}

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;
		private Boolean SetField<T>(ref T field, T value, String propertyName)
		{
			if(EqualityComparer<T>.Default.Equals(field, value))
				return false;

			field = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			return true;
		}
		#endregion INotifyPropertyChanged
	}
}