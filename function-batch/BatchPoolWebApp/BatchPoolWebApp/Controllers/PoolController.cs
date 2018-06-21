using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatchPoolWebApp.Models;
using BatchPoolWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace BatchPoolWebApp.Controllers
{
    public class PoolController : Controller
    {
        private readonly IBatchService _batchService;

        public PoolController(IBatchService batchService)
        {
            _batchService = batchService;
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }

        public async Task<IActionResult> Index()
        {
            var staticPoolItems = await _batchService.GetPoolDataAsync();

            var poolsInBatch = _batchService.GetPoolsInBatch();
            IList<PoolModel> poolItems = new List<PoolModel>();

            foreach (var pool in poolsInBatch)
            {
                poolItems.Add(new PoolModel { PoolId = pool.Id, AllocationState = pool.AllocationState});
            }

            var poolViewModel = new PoolViewModel()
            {
                PoolItems = poolItems
            };
            
            return View(poolViewModel);
        }
    }
}