using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBomber.Contracts; // To use ScenarioProps.
using NBomber.CSharp; // To use Scenario, Simulation, NBomberRunner.
using NBomber.Http; // To use HttpMetricsPlugin.
using NBomber.Http.CSharp; // To use Http.
using NBomber.Plugins.Network.Ping;
using Flurl.Http;
using System.Net;
using System.Net.Http;

namespace DonorNBomberApp
{
    internal class Quote
    {
        public string id { get; set; }
        public int lengthCm { get; set; }
        public int widthCm { get; set; }
        public int heightCm { get; set; }
        public int weightKg { get; set; }
        public string countryFrom { get; set; } = "";
        public string countryTo { get; set; } = "";
    };
    internal class Program
    {
        private async static Task Main(string[] args)
        {
            // FIRST WE TEST THE SERVICE

            string id = string.Empty;
            string apiUrl = "https://localhost:7190/api/Quote"; // Replace with your API endpoint
            IFlurlResponse response = null;

            try
            {
                var postContent = new
                {
                    lengthCm = 70,
                    widthCm = 70,
                    heightCm = 70,
                    weightKg = 4,
                    countryFrom = "BE",
                    countryTo = "BE"
                };

                // Make a POST request using Flurl
                response = await apiUrl
                    .WithHeader("Content-Type", "application/json") // Optional: Set the request header
                    .PostJsonAsync(postContent);

                // Check if the request was successful
                if ((HttpStatusCode)response.StatusCode == HttpStatusCode.Created)
                {
                    // var responseBody = await response.GetStringAsync();
                    var responseBody = await response.GetJsonAsync<Quote>();
                    id = responseBody.id;
                    Console.WriteLine("POST request successful. Response:");
                    Console.WriteLine(responseBody);
                }
                else
                {
                    Console.WriteLine($"POST request failed with status code: {response.StatusCode}");
                    return;
                }
            }
            catch (FlurlHttpTimeoutException ex)
            {
                Console.WriteLine($"Timeout error: {ex.Message}");
                return;
            }
            catch (FlurlHttpException ex)
            {
                Console.WriteLine($"HTTP error: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return;
            }

            var getUrl = apiUrl + "/" + id;
            // Make a GET request using Flurl: waarom zouden we dit doen?
            var quote = await getUrl
                .WithHeader("Content-Type", "application/json") // Optional: Set the request header
                .GetJsonAsync<Quote>();

            // THEN WE "BOMBARD" THE SERVICE 

            using HttpClient client = new();

            LoadSimulation[] loads = [
             // WARM UP IS REQUIRED for .NET!!!
             // Ramp up to 50 RPS during one minute.
                Simulation.RampingInject(rate: 5,
                interval: TimeSpan.FromSeconds(10),
                during: TimeSpan.FromMinutes(1)),
             // Maintain 50 RPS for another minute.
                Simulation.Inject(rate: 5,
                interval: TimeSpan.FromSeconds(10),
                during: TimeSpan.FromMinutes(1)),
             // Ramp down to 0 RPS during one minute.
                Simulation.RampingInject(rate: 0,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(1))
            ];
            ScenarioProps scenario = Scenario.Create(
              name: "http_scenario",
              run: async context =>
              {
                  HttpRequestMessage request = Http.CreateRequest(
                    "GET", "https://localhost:7190/api/Quote/" + id)
                    .WithHeader("Accept", "application/json");
                  // Use WithHeader and WithBody to send a JSON payload.
                  // .WithHeader("Content-Type", "application/json")
                  // .WithBody(new StringContent("{ some JSON }", Encoding.UTF8, "application/json"));
                  Response<HttpResponseMessage> response = await Http.Send(client, request);
                  return response;
              })
              .WithoutWarmUp()
              .WithLoadSimulations(loads);
            NBomberRunner
              .RegisterScenarios(scenario)
              .WithWorkerPlugins(
                new PingPlugin(PingPluginConfig.CreateDefault("localhost")),
                new HttpMetricsPlugin([NBomber.Http.HttpVersion.Version1])
              )
              .Run();
        }
    }
}
