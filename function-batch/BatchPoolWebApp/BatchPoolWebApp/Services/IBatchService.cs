using BatchPoolWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BatchPoolWebApp.Services
{
    public interface IBatchService
    {
        Task<PoolModel[]> GetPoolDataAsync();
    }
}
