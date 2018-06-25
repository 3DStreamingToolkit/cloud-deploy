using Cloud3DSTKDeploymentAPI.Controllers;
using Cloud3DSTKDeploymentAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Cloud3DSTKDeploymentAPI.Tests
{
    [TestClass]
    public class Cloud3DSTKApiTests
    {
        private IConfiguration configuration;

        private void SetupConfiguration()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            configuration = configurationBuilder.Build();
        }

        [TestMethod]
        public async Task CreateAPIReturnsInvalidRequestWhenNoBody()
        {  
            // Read the appsettings json
            this.SetupConfiguration();

            var batchService = new BatchService(configuration);
            var controller = new Cloud3DSTKApiController(batchService);

            var emptyBody = new JObject();

            var result = await controller.Post(emptyBody);
            
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }
    }
}
