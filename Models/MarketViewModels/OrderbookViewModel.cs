﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using via_jsonrpc;

namespace viafront3.Models.MarketViewModels
{
    public class OrderbookViewModel : BaseViewModel
    {
        public string Market { get; set; }

        public string MarketNice { get; set; }

        public OrderbookPartialViewModel OrderBook { get; set; }
    }

    public class OrderbookPartialViewModel
    {
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }

        public string AmountUnit { get; set; }

        public string PriceUnit { get; set; }

        public OrderDepth OrderDepth { get; set; }
    }
}
