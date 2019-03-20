using System;
using System.Collections.Generic;
using System.Linq;

namespace viafront3.Models.ApiViewModels
{
    public struct ApiToken
    {
        public string Token;
    }

    public struct ApiDevice
    {
        public bool Completed;
        public string DeviceKey;
        public string DeviceSecret;
    }

    public class ApiAccountCreate
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string DeviceName { get; set; }
    }

    public class ApiDeviceCreate
    {
        public string Email { get; set; }
        public string DeviceName { get; set; }
    }

    public class ApiAuth
    {
        public String Key { get; set; }
        public String Signature { get; set; }
        public long Nonce { get; set; }
    }
}
