using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace viafront3.Models.ManageViewModels
{
    public class ApiViewModel : BaseViewModel
    {
        public IList<Device> Devices { get; set; }
        public string DeleteDeviceKey { get; set; }
    }

    public class ApiCreateViewModel : BaseViewModel
    {
        [Required]
        public string DeviceName { get; set; }

        public bool TwoFactorRequired { get; set; }
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Authenticator code")]
        public string TwoFactorCode { get; set; }
    }
}
