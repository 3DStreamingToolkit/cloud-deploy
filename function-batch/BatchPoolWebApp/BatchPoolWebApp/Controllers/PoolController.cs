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

        public async Task<IActionResult> Index()
        {
            var poolItems = await _batchService.GetPoolDataAsync();

            var poolViewModel = new PoolViewModel()
            {
                PoolItems = poolItems
            };

            return View(poolViewModel);
        }
    }
}