using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace BatchPoolWebApp.Models
{
    public class PoolCreateModel
    {
        [DisplayName("Vnet Name")]
        public string VnetName { get; set; }

        [DisplayName("Pool Count")]
        public int PoolCount { get; set; }

        [DisplayName("Pool Id Prefix")]
        public string PoolIdPrefix { get; set; }

        [DisplayName("Job Id")]
        public string JobId { get; set; }

        [DisplayName("Node Count")]
        public int NodeCount { get; set; }
    }
}
