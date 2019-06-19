using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using viafront3.Models.ManageViewModels;

namespace viafront3.Models.ManageViewModels
{
    public class ApiViewModel : BaseViewModel
    {
        public IList<ApiKey> ApiKeys { get; set; }
        public string DeleteApiKey { get; set; }
    }

    public class ApiCreateViewModel : TwoFactorRequiredViewModel
    {
        [Required]
        public string DeviceName { get; set; }
    }
}
