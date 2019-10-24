using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace viafront3.Services
{
    public interface IAppVersion
    {
        string Version { get; }
    }

    public class AppVersion : IAppVersion
    {
        public string Version =>
            Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
