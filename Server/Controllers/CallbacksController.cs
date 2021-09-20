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

        [HttpPost("inbound/messaging")]
        public async Task<ActionResult> MessagesInbound()
        {
            _logger.LogInformation("Received message callback request.");

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var message = JsonConvert.DeserializeObject<IEnumerable<BandwidthCallbackMessage>>(body).First();

            _logger.LogInformation($"{message.Type} {message.Description}");

            switch (message.Type)
            {
                case "message-received":
                    _logger.LogInformation(message.Message.Media != null
                        ? $"Message received with '{message.Message.Media.Count}' media." : "Message received with no media.");
                    return StatusCode(200);
                default:
                    _logger.LogInformation("Message type does not match endpoint. This endpoint is used for inbound messages only.\n      Outbound message callbacks should be sent to /callbacks/outbound/messaging.");
                    break;
            }

            return new OkResult();
        }

        [HttpPost("outbound/messaging")]
        public async Task<ActionResult> MessagesOutbound()
        {
            _logger.LogInformation("Received message callback request.");

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var message = JsonConvert.DeserializeObject<IEnumerable<BandwidthCallbackMessage>>(body).First();

            _logger.LogInformation($"{message.Type} {message.Description}");

            switch (message.Type)
            {
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
                    _logger.LogInformation("Message type does not match endpoint. This endpoint is used for outbound status callbacks only.\n      Inbound message callbacks should be sent to /callbacks/inbound/messaging.");
                    break;
                
            }

            return new OkResult();
        }
    }
}
