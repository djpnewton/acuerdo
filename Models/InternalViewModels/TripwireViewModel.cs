using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using viafront3.Services;

namespace viafront3.Models.InternalViewModels
{
    public class TripwireViewModel : BaseViewModel
    {
        public ITripwire Tripwire { get; set; }
        public TripwireSettings Settings { get; set; }
        public TripwireStats Stats { get; set; }
    }
}
