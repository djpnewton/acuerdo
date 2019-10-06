using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace viafront3.Models.ManageViewModels
{
    public class KycViewModel : BaseViewModel
    {
        public int LevelNum { get; set; }
        public KycLevel Level { get; set; }
        public String WithdrawalTotalThisPeriod { get; set; }
        public KycSettings KycSettings { get; set; }
        public string KycRequestUrl { get; set; }
        public string KycRequestStatus { get; set; }
    }
}
