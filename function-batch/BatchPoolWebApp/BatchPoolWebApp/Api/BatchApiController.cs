using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BatchPoolWebApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

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
        public async Task<IActionResult> Post(
            [FromBody] JObject jsonBody)
        {
            var signalingServer = jsonBody["signalingServer"]?.ToObject<string>();
            var signalingServerPort = jsonBody["signalingServerPort"]?.ToObject<int>();
            string linuxPoolId = jsonBody["linuxPoolId"]?.ToObject<string>() ?? "LinuxPool";
            string windowsPoolId = jsonBody["windowsPoolId"]?.ToObject<string>() ?? "WindowsPool";
            int numberOfDedicatedLinuxNodes = jsonBody["numberOfDedicatedLinuxNodes"]?.ToObject<int>() ?? 1;
            int numberOfDedicatedWindowsNodes = jsonBody["numberOfDedicatedWindowsNodes"]?.ToObject<int>() ?? 1;
            string windowsJobId = jsonBody["windowsJobId"]?.ToObject<string>() ?? "3DSTKWindowsJob";
            string linuxJobId = jsonBody["linuxJobId"]?.ToObject<string>() ?? "3DSTKTURNJob";

            if (string.IsNullOrEmpty(signalingServer) || !signalingServerPort.HasValue)
                return BadRequest("Signaling is required");

            var linuxPool = _batchService.GetPoolsInBatch().FirstOrDefault((s) => s.Id == linuxPoolId);
            if (linuxPool == null)
            {
                await _batchService.CreateLinuxPool(linuxPoolId, numberOfDedicatedLinuxNodes);
                await _batchService.CreateJobAsync(linuxJobId, linuxPoolId);

                var taskResults = await _batchService.MonitorTasks(linuxJobId, new TimeSpan(0, 20, 0));

                if(!taskResults)
                {
                    await _batchService.DeleteJobAsync(linuxJobId);
                    await _batchService.DeletePoolAsync(linuxPoolId);
                    return BadRequest("Linux tasks failed!");
                }
            }

            var windowsPool = _batchService.GetPoolsInBatch().FirstOrDefault((s) => s.Id == windowsPoolId);
            if (windowsPool == null)
            {
                await _batchService.CreateWindowsPool(linuxPoolId, windowsPoolId, numberOfDedicatedWindowsNodes, signalingServer, signalingServerPort.Value);
            }

            // await _batchService.CreateJobAsync(windowsJobId, windowsPoolId);
           // await _batchService.AddWindowsTasksAsync(linuxPoolId, windowsJobId, signalingServer, signalingServerPort.Value);

            // await _batchService.MonitorTasks(windowsJobId, new TimeSpan(0, 20, 0));

            return Ok();
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
