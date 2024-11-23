using System;

namespace Plugin.WcfServer.Parser
{
	internal class ValidationResult
	{
		public Boolean RefreshRequired { get; private set; }

		public Boolean IsValid { get; private set; }

		public String ErrorMessage { get; private set; }

		public ValidationResult(Boolean isValid, Boolean refreshRequired)
			: this(isValid, refreshRequired, null)
		{ }

		public ValidationResult(Boolean isValid, Boolean refreshRequired, String errorMessage)
		{
			this.IsValid = isValid;
			this.RefreshRequired = refreshRequired;
			this.ErrorMessage = errorMessage;
		}
	}
}