using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace viafront3
{
    class LockFile
    {
        const string Prefix = "ACUERDO_";
        readonly ILogger _logger = null;
        readonly string _name = null;

        string GetUserTempPath()
        {
            string path = Path.GetTempPath();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                path = Path.Combine(path, Environment.UserName) + Path.DirectorySeparatorChar; // CreateDirectory seems to need the trailing slash
                Directory.CreateDirectory(path);
                _logger.LogInformation($"created lockfile directory ({path})");
            }
            return path;
        }

        public string MkPath()
        {
            return Path.Join(GetUserTempPath(), Prefix + _name);
        }

        public LockFile(ILogger logger, string name)
        {
            _logger = logger;
            _name = name;
        }

        public bool CreateIfNotPresent(string contents)
        {
            var path = MkPath();
            if (File.Exists(path))
                return false;
            using (var f = File.CreateText(path))
            {
                f.Write(contents);
                f.Flush();
                f.Close();
            }
            return true;
        }

        public bool RemoveIfPresent()
        {
            var path = MkPath();
            if (!File.Exists(path))
                return false;
            File.Delete(path);
            return true;
        }

        public bool IsPresent()
        {
            return File.Exists(MkPath());
        }
    }
}