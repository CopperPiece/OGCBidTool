using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OGCBidTool.Models
{
    public class Roller
    {
        public string Name { get; set; }
        public UInt32 AdjustedValue { get; set; }
        public UInt32 Value { get; set; }
        public UInt32 RollMax { get; set; }
        public UInt32 RA60 { get; set; }
        public string Rank { get; set; }
        public bool IsSelected { get; set; }
    }
}
