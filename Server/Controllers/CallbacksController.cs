using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bandwidth.Standard.Messaging.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CallbacksController : ControllerBase
    {
        private readonly ILogger<CallbacksController> _logger;

        public CallbacksController(ILogger<CallbacksController> logger)
        {
            _logger = logger;
        }

        [HttpPost("messageCallback")]
        public async Task<ActionResult> Messages()
        {
            _logger.LogInformation("Received message callback request.");

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var message = JsonConvert.DeserializeObject<IEnumerable<BandwidthCallbackMessage>>(body).First();

            _logger.LogInformation($"{message.Type} {message.Description}");

            switch (message.Type)
            {
                case "message-received":
                    _logger.LogInformation(message.Message.Media.Any()
                        ? $"Message received with '{message.Message.Media.Count}' media." : "Message received with no media.");
                    break;
                case "message-sending":
                    _logger.LogInformation("Message is sending to the carrier.");
                    break;
                case "message-delivered":
                    _logger.LogInformation("Message delivered from Bandwidth's network.");
                    break;
                case "message-failed":
                    _logger.LogInformation("Messaged failed to be delivered.");
                    break;
                default:
                    _logger.LogInformation("Unknown message type received.");
                    break;
            }

            return new OkResult();
        }
    }
}
