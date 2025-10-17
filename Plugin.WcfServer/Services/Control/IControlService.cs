using System;
using System.ServiceModel;

namespace Plugin.WcfServer.Services.Control
{
	/// <summary>Control WCF service interface</summary>
	[ServiceContract]
	public interface IControlService
	{
		/// <summary>Connect child process to control process</summary>
		/// <param name="processId">Child process ID</param>
		/// <param name="endpointAddress">Client process address</param>
		/// <returns>Host process ID</returns>
		[OperationContract(IsOneWay = false)]
		Int32 Connect(Int32 processId, String endpointAddress);

		/// <summary>Disconnect child process from control process</summary>
		/// <param name="processId">Child process ID</param>
		[OperationContract(IsOneWay = true)]
		void Disconnect(Int32 processId);

		/// <summary>Check main host is alive</summary>
		/// <param name="processId">Child process ID</param>
		/// <returns>Control process ID</returns>
		[OperationContract(IsOneWay = false)]
		Int32 Ping(Int32 processId);
	}
}