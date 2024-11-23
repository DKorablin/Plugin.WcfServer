using System;
using System.Runtime.Serialization;

namespace Plugin.WcfServer.Data
{
	[DataContract]
	public class ConnectRequest
	{
		[DataMember]
		public Int32 ProcessId { get; private set; }

		[DataMember]
		public Boolean IsAdministrator { get; private set; }

		[DataMember]
		public String EndpointAddress { get; private set; }
	}
}