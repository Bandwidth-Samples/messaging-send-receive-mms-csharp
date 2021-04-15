using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bandwidth.Standard;
using Bandwidth.Standard.Http.Response;
using Bandwidth.Standard.Messaging.Exceptions;
using Bandwidth.Standard.Messaging.Models;

namespace SendReceiveMMS
{
    class Program
    {
        // Bandwidth provided messaging token.
        private static readonly string Token = System.Environment.GetEnvironmentVariable("BW_USERNAME");
        
        // Bandwidth provided messaging secret.
        private static readonly string Secret = System.Environment.GetEnvironmentVariable("BW_PASSWORD");

        // Bandwidth provided application id.
        private static readonly string ApplicationId = System.Environment.GetEnvironmentVariable("BW_MESSAGING_APPLICATION_ID");

        // Bandwidth provided account id.
        private static readonly string AccountId = System.Environment.GetEnvironmentVariable("BW_ACCOUNT_ID");

        // The phone number to send the message from.
        private static readonly string From = System.Environment.GetEnvironmentVariable("BW_NUMBER");
        
        // The phone number to send the message to.
        private static readonly string To = System.Environment.GetEnvironmentVariable("USER_NUMBER");

        // The media to send to the "to" phone number.
        private static readonly string Media = "https://cdn2.thecatapi.com/images/MTY3ODIyMQ.jpg";

        static async Task Main(string[] args)
        {
            // Creates a Bandwidth client instance for creating messages.
            var client = new BandwidthClient.Builder()
                .Environment(Bandwidth.Standard.Environment.Production)
                .MessagingBasicAuthCredentials(Token, Secret)
                .Build();

            // A message request containing the required information to create a message using the client.
            var request = new MessageRequest() {
                ApplicationId = ApplicationId,
                To = new List<string> { To },
                From = From,
                Media = new List<string> { Media }
            };

            // Creates and sends an MMS message with the provided message request.
            try
            {
                var response = await client.Messaging.APIController.CreateMessageAsync(AccountId, request);
                Console.WriteLine($"Create message response status code '{response.StatusCode}'.");
            }
            catch (MessagingException e)
            {
                var body = ((HttpStringResponse)e.HttpContext.Response).Body;
                Console.WriteLine($"A messaging exception has occurred. {e.Message}");
                Console.WriteLine(body);
                System.Environment.Exit(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unknown exception has occurred. {e.Message}");
                System.Environment.Exit(-1);
            }
        }
    }
}
