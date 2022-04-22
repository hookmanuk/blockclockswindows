using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockClocksWindows
{
    public class transaction
    {
        public string hash { get; set; }
        public input[] inputs { get; set; }
        public output[] outputs { get; set; }
    }

    public class input
    {
        public string address { get; set; }
        public amount[] amount { get; set; }
        public string tx_hash { get; set; }
        public int output_index { get; set; }
        public bool collateral { get; set; }
        public object data_hash { get; set; }
    }

    public class amount
    {
        public string unit { get; set; }
        public string quantity { get; set; }
    }

    public class output
    {
        public string address { get; set; }
        public amount[] amount { get; set; }
        public int output_index { get; set; }
        public object data_hash { get; set; }
    }
}
