using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using via_jsonrpc;

namespace viafront3.Models.ManageViewModels
{
    public class DepositViewModel : BaseViewModel
    {
        public string Asset { get; set; }
        public string DepositAddress { get; set; }
    }
}
