using Microsoft.Azure.Batch.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BatchPoolWebApp.Models
{
    public class PoolModel
    {
        public string PoolId { get; set; }
        public AllocationState? AllocationState { get; set; }
    }
}
