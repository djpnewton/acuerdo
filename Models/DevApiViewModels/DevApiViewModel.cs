using System;
using System.Collections.Generic;
using System.Linq;

namespace viafront3.Models.DevApiViewModels
{
    public class DevApiUserCreate
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool SendEmail { get; set; }
    }
}
