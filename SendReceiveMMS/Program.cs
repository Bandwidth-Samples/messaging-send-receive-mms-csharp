using System.Linq.Expressions;
using Bandwidth.Standard;
using Bandwidth.Standard.Api;
using Bandwidth.Standard.Client;
using Bandwidth.Standard.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Declare the variables outside the try block
string BW_USERNAME = null;
string BW_PASSWORD = null;
string BW_MESSAGING_APPLICATION_ID = null;
string BW_ACCOUNT_ID = null;
string BW_NUMBER = null;
string USER_NUMBER = null;
string Media = "https://cdn2.thecatapi.com/images/MTY3ODIyMQ.jpg";

//Setting up environment variables
try
{
    BW_USERNAME = System.Environment.GetEnvironmentVariable("BW_USERNAME");
    BW_PASSWORD = System.Environment.GetEnvironmentVariable("BW_PASSWORD");
    BW_MESSAGING_APPLICATION_ID = System.Environment.GetEnvironmentVariable("BW_MESSAGING_APPLICATION_ID");
    BW_ACCOUNT_ID = System.Environment.GetEnvironmentVariable("BW_ACCOUNT_ID");
    BW_NUMBER = System.Environment.GetEnvironmentVariable("BW_NUMBER");
    USER_NUMBER = System.Environment.GetEnvironmentVariable("USER_NUMBER");
}
catch (System.Exception)
{
    Console.WriteLine("Please set the environmental variables defined in the README");
    throw;
}

Configuration configuration = new Configuration();
configuration.Username = BW_USERNAME;
configuration.Password = BW_PASSWORD;

app.MapPost("/sendMessages", async (HttpContext context) =>
    {
        // Deserialize the request a list of key valued pairs
        var requestBody = new Dictionary<string, string>();
        using(var streamReader = new StreamReader(context.Request.Body))
        {
            var body = await streamReader.ReadToEndAsync();
            requestBody = JsonConvert.DeserializeObject<Dictionary<string,string>>(body);
        }

        context.Request.ContentType = "application/json";

        MessageRequest request = new MessageRequest(
            applicationId: BW_MESSAGING_APPLICATION_ID,
            to: new List<string> { requestBody["to"] },
            from: BW_NUMBER,
            text: requestBody["text"],
            media: new List<string> { Media }
        );

        MessagesApi apiInstance = new MessagesApi(configuration);
        try
        {
            // Send a message
            var result = await apiInstance.CreateMessageAsync(BW_ACCOUNT_ID, request);
            Console.WriteLine(result);
        }
        catch (ApiException e)
        {
            Console.WriteLine("Exception when calling MessagesApi.CreateMessage: " + e.Message);
        }
    }
);

app.MapPost("/callbacks/outbound/messaging/status", async (HttpContext context) =>
{
    var requestBody = new List<object>();
    using(var streamReader = new StreamReader(context.Request.Body))
    {
        var body = await streamReader.ReadToEndAsync();
        requestBody = JsonConvert.DeserializeObject<List<object>>(body);
    }

    // Access the "type" property of the first object in the list
    var type = (string)((dynamic)requestBody[0]).type;
    
    // switch case statement
    switch (type)
    {
        case "message-sending":
            Console.WriteLine("MMS message is sending.");
            break;
        case "message-delivered":
            Console.WriteLine("Your message has been handed off to the Bandwidth's MMSC network, but has not been confirmed at the downstream carrier.");
            break;
        case "message-failed":
            Console.WriteLine("For MMS and Group Messages, you will only receive this callback if you have enabled delivery receipts on MMS.");
            break;
        default:
            Console.WriteLine("Message type does not match endpoint. This endpoint is used for message status callbacks only.");
            break;
    }
});

app.MapPost("/callbacks/inbound/messaging", async (HttpContext context) =>
{
    var requestBody = new List<object>();
    using(var streamReader = new StreamReader(context.Request.Body))
    {
        var body = await streamReader.ReadToEndAsync();
        requestBody = JsonConvert.DeserializeObject<List<object>>(body);
    }

    // Access the "type" property of the first object in the list
    var type = (string)((dynamic)requestBody[0]).type;
    
    if(type.Equals("message-received"))
    {
        var to = (string)((dynamic)requestBody[0]).message.to[0];
        var from = (string)((dynamic)requestBody[0]).message.from;
        var text = (string)((dynamic)requestBody[0]).message.text;

        var mediaApi = new MediaApi(configuration);

        string mediaId = null;
        string mediaName = null;
        foreach ( string item in (JArray)((dynamic)requestBody[0]).message.media)
        {
            mediaId = item.Split(new string[] { "media/" }, StringSplitOptions.None).Last(); // gets the media ID used for GET media
            string[] mediaParts = mediaId.Split('/');
            mediaName = mediaParts[mediaParts.Length - 1]; // gets the name of the downloaded media file
        }

        if(!mediaName.Contains(".xml"))
        {
            var mediaFile = mediaApi.GetMedia(BW_ACCOUNT_ID, mediaId);
            using (var fileStream = File.Create(mediaName))
            {
                mediaFile.CopyTo(fileStream);
            }
        }
    }
    else
    {
        Console.WriteLine("Message type does not match endpoint. This endpoint is used for inbound messages only.");
        Console.WriteLine("Outbound message callbacks should be sent to /callbacks/outbound/messaging.");
    }
});

app.Run();
