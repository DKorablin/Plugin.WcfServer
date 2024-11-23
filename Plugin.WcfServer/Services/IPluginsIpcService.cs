using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using Plugin.WcfServer.Data;

namespace Plugin.WcfServer.Services
{
	/// <summary>Сервис для IPC взаимодействия</summary>
	[ServiceContract]
	public interface IPluginsIpcService
	{
		/// <summary>Получить список всех плагинов, которые загружены в текущий хост</summary>
		/// <returns>Список всех плагинов, загруженные в текущий хост</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "Plugins", BodyStyle = WebMessageBodyStyle.Bare)]
		PluginData[] GetPlugins();

		/// <summary>Получить информацию о плагине со всеми членами плагина</summary>
		/// <param name="id">Идентификатор плагина</param>
		/// <returns>Информация о плагине в JSON формате</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "Plugins/{id}", BodyStyle = WebMessageBodyStyle.Bare)]
		String GetPlugin(String id);

		/// <summary>Вызвать свойство или метод плагина, котоые не ожидают на вход аргументов</summary>
		/// <param name="id">Идентификатор плагина</param>
		/// <param name="memberName">Наименование свойства или метода, который не ожидает входящих параметров</param>
		/// <returns>Ответ от плагина в JSON формате</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "Plugins/{id}/{*memberName}", BodyStyle = WebMessageBodyStyle.Bare)]
		String InvokeGetMember(String id, String memberName);

		/// <summary>Вызвать метод плагина, передав в теле HTTP запроса данные в виде JSON в качестве аргументов метода плагина</summary>
		/// <param name="id">Идентификатор плагина</param>
		/// <param name="memberName">Наименование метода в плагине</param>
		/// <returns>Ответ от плагина в JSON формате</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "POST", UriTemplate = "Plugins/{id}/{memberName}/{*payload}", BodyStyle = WebMessageBodyStyle.Bare)]
		String InvokeMember(String id, String memberName, String payload);
	}
}