using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using SAL.Flatbed;

namespace Plugin.WcfServer
{
	public class Plugin : IPlugin, IPluginSettings<PluginSettings>
	{
		private PluginSettings _settings;
		private ServiceFactory _server;

		internal static ITraceSource Trace { get; private set; }

		internal IHost Host { get; }
		internal static Plugin SPlugin { get; private set; }

		/// <summary>Settings for interaction from host</summary>
		Object IPluginSettings.Settings => this.Settings;

		/// <summary>Settings for interaction from plugin</summary>
		public PluginSettings Settings
		{
			get
			{
				if(this._settings == null)
				{
					this._settings = new PluginSettings(this.Host);
					this.Host.Plugins.Settings(this).LoadAssemblyParameters(this._settings);
				}
				return this._settings;
			}
		}

		public Boolean IsStarted => this._server.State == CommunicationState.Opened;

		public Plugin(IHost host, ITraceSource trace)
		{
			this.Host = host ?? throw new ArgumentNullException(nameof(host));
			Plugin.Trace = trace ?? throw new ArgumentNullException(nameof(trace));
			Plugin.SPlugin = this;//HACK: For access
		}

		Boolean IPlugin.OnConnection(ConnectMode mode)
		{
			this._server = new ServiceFactory();
			this._server.Connected += this.Server_Connected;
			this._server.Connect(this.Settings.GetHostUrl(), this.Settings.Type);

			return true;
		}

		Boolean IPlugin.OnDisconnection(DisconnectMode mode)
		{
			if(this._server != null)
			{
				this._server.Dispose();
				this._server = null;
			}
			return true;
		}

		private void Server_Connected(Object sender, EventArgs e)
			=> Plugin.Trace.TraceEvent(TraceEventType.Start, 1, "Started at Url:\r\n\t{0}", String.Join("\r\n\t", this._server.GetHostEndpoints().ToArray()));

		internal static Type GetType(String typeName)
		{
			Type type = Type.GetType(typeName, false);
			if(type != null)
				return type;

			foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = a.GetType(typeName);
				if(type != null)
					return type;
			}
			return null;
		}
	}
}