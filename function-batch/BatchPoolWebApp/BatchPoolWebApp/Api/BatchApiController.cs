using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatchPoolWebApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BatchPoolWebApp.Api
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class BatchApiController : Controller
    {
        private readonly IBatchService _batchService;

        public BatchApiController(IBatchService batchService)
        {
            _batchService = batchService;
        }

        // GET: api/BatchApi
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/BatchApi/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/BatchApi
        [HttpPost]
        public IEnumerable<string> Post([FromBody]string value)
        {
            _batchService.CreateLinuxPool("poolId1", 1);
            return new string[] { "value1", "value2" };
        }

        // PUT: api/BatchApi/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
