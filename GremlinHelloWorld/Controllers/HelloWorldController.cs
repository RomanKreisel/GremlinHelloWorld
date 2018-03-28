using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GremlinHelloWorld.Controllers
{
    [Route("api/[controller]")]
    public class HelloWorldController : Controller
    {
        private IConfiguration configuration;

        public HelloWorldController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpGet]
        public Dictionary<string, object> Get()
        {
            var result = new Dictionary<string, object>();
            try
            {
                var gremlinServiceEndpoint = configuration["Database:GremlinEndpointHostname"];
                var gremlinServicePort = Int32.Parse(configuration["Database:GremlinEndpointPort"]);
                var gremlinUsername = configuration["Database:GremlinUsername"];
                var gremlinPassword = configuration["Database:GremlinPassword"];
                var partitionKey = configuration["Database:PartitionKey"];

                var gremlinServer = new GremlinServer(gremlinServiceEndpoint, gremlinServicePort, true, gremlinUsername, gremlinPassword);

                var random = new Random();

                //NOTE: Downgrading to GraphSON2Reader/Writer is necessary, when connecting to a Cosmos DB (GraphSON3 not supported?)
                var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);



                gremlinClient.SubmitAsync("g.V().drop()").Wait();

                gremlinClient.SubmitAsync($"g.addV('Person').property('{partitionKey}', '{random.Next()}').property('name', 'John Doe')").Wait();

                //NOTE: Creating a new graph and traversal works fine, even with a defined RemoteConnection. The connection is not yet established
                var graph = new Gremlin.Net.Structure.Graph();
                var connection = new DriverRemoteConnection(gremlinClient);
                var traversal = graph.Traversal()
                    .WithRemote(connection)
                    .V().HasLabel("Person").Count();


                var count = traversal.Next(); //NOTE: this throws a NullReferenceException

                //NOTE: expected result would be "1", but the call above throws an exception<
                result.Add("count", count);
            }
            catch (Exception e)
            {
                result.Add("exception", e);
            }
            return result;
        }
    }
}
