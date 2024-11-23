using System;
using System.Collections.Generic;
using Plugin.WcfServer.Extensions;
using SAL.Flatbed;

namespace Plugin.WcfServer.Parser
{
	internal class PluginMethodWrapper
	{
		private readonly IPluginDescription _plugin;
		private readonly IPluginMethodInfo _method;

		internal IList<PluginParameterWrapper> InputParameters { get; private set; } = new List<PluginParameterWrapper>();

		private IList<PluginParameterWrapper> OtherParameters { get; set; } = new List<PluginParameterWrapper>();

		internal Boolean Valid
			=> this.InputParameters.Find(new Predicate<PluginParameterWrapper>(this.IsServiceMemberInValid)) == null
				&& this.OtherParameters.Find(new Predicate<PluginParameterWrapper>(this.IsServiceMemberInValid)) == null;

		internal PluginMethodWrapper(IPluginDescription plugin, IPluginMethodInfo method)
		{
			this._plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
			this._method = method ?? throw new ArgumentNullException(nameof(method));

			this.AnalyzeParameters();
		}

		private void AnalyzeParameters()
		{
			foreach(IPluginParameterInfo parameter in this._method.GetParameters())
				this.InputParameters.Add(new PluginParameterWrapper(parameter));
		}

		public VariableWrapper[] GetVariables()
		{
			VariableWrapper[] array = new VariableWrapper[this.InputParameters.Count];
			Int32 num = 0;
			foreach(PluginParameterWrapper current in this.InputParameters)
			{
				array[num] = new VariableWrapper(current);
				array[num].SetServiceMethodInfo(this);
				num++;
			}
			return array;
		}

		private Boolean IsServiceMemberInValid(PluginParameterWrapper member)
			=> !member.IsValid;
	}
}