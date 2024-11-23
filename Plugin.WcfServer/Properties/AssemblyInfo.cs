using System.Reflection;
using System.Runtime.InteropServices;

[assembly: ComVisible(false)]
[assembly: Guid("026c98e3-6331-4f4c-afde-d449845801ac")]
[assembly: System.CLSCompliant(true)]

#if NETCOREAPP
[assembly: AssemblyMetadata("ProjectUrl", "https://dkorablin.ru/project/Default.aspx?File=122")]
#else

[assembly: AssemblyTitle("Plugin.WCF Server")]
[assembly: AssemblyDescription("WCF server for remote plugin calling")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("Danila Korablin")]
[assembly: AssemblyProduct("Plugin.WCF Server")]
[assembly: AssemblyCopyright("Copyright © Danila Korablin 2019-2024")]
#endif