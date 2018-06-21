using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BatchPoolWebApp.Models
{
    public enum AllocationState
    {
        Steady = 0,
        Resizing = 1,
        Stopping = 2
    }

    public class PoolModel
    {
        public string PoolId { get; set; }
        public AllocationState AllocationState { get; set; }
    }
}
