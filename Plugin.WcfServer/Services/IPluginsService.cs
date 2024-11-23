using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using Plugin.WcfServer.Data;

namespace Plugin.WcfServer.Services
{
	/// <summary>Сервис информации и манипуляций плагинов</summary>
	[ServiceContract]
	public interface IPluginsService : IPluginsIpcService
	{
		/// <summary>Получить список инстанстов, на которых запущено аналогичное ядро</summary>
		/// <returns></returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "Instance", BodyStyle = WebMessageBodyStyle.Bare)]
		Int32[] GetInstance();

		/// <summary>Получить список всех плагинов, которые загружены в текущий хост</summary>
		/// <param name="instance">Получить плагины в определённом инстансе</param>
		/// <returns>Список всех плагинов, загруженные в текущий хост</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "{instance}/Plugins", BodyStyle = WebMessageBodyStyle.Bare)]
		PluginData[] GetIpcPlugins(String instance);

		/// <summary>Получить информацию о плагине со всеми членами плагина</summary>
		/// <param name="instance">Получить плагины в определённом инстансе</param>
		/// <param name="id">Идентификатор плагина</param>
		/// <returns>Информация о плагине в JSON формате</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "{instance}/Plugins/{id}", BodyStyle = WebMessageBodyStyle.Bare)]
		String GetIpcPlugin(String instance, String id);

		/// <summary>Вызвать свойство или метод плагина, котоые не ожидают на вход аргументов</summary>
		/// <param name="instance">Выполнить метод в определённом инстансе</param>
		/// <param name="id">Идентификатор плагина</param>
		/// <param name="memberName">Наименование свойства или метода, который не ожидает входящих параметров</param>
		/// <returns>Ответ от плагина в JSON формате</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "GET", UriTemplate = "{instance}/Plugins/{id}/{memberName}", BodyStyle = WebMessageBodyStyle.Bare)]
		String InvokeIpcGetMember(String instance, String id, String memberName);

		/// <summary>Вызвать метод плагина, передав в теле HTTP запроса данные в виде JSON в качестве аргументов метода плагина</summary>
		/// <param name="instance">Выполнить метод в определённом инстансе</param>
		/// <param name="id">Идентификатор плагина</param>
		/// <param name="memberName">Наименование свойства или метода, который не ожидает входящих параметров</param>
		/// <returns>Ответ от плагина в JSON формате</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "POST", UriTemplate = "{instance}/Plugins/{id}/{memberName}", BodyStyle = WebMessageBodyStyle.Bare)]
		String InvokeIpcMember(String instance, String id, String memberName);

		/// <summary>Вызвать метод плагина, передав в теле HTTP запроса данные в виде JSON в качестве аргументов метода плагина</summary>
		/// <param name="id">Идентификатор плагина</param>
		/// <param name="memberName">Наименование свойства или метода, который не ожидает входящих параметров</param>
		/// <returns>Первый найденный ответ от плагина в JSON формате</returns>
		[OperationContract(IsOneWay = false)]
		[WebInvoke(Method = "POST", UriTemplate = "Plugin/{id}/{memberName}", BodyStyle = WebMessageBodyStyle.Bare)]
		String FindAndInvokeIpcMember(String id, String memberName);
	}
}