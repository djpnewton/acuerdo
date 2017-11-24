using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using via_jsonrpc;

namespace viafront3.Models.InternalViewModels
{
    public class TestWebsocketViewModel : BaseViewModel
    {
        public string WebsocketToken { get; set; }
        public string WebsocketUrl { get; set; }
    }
}
