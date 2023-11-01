using Bandwidth.Standard.Api;
using Bandwidth.Standard.Client;
using Bandwidth.Standard.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string BW_USERNAME;
string BW_PASSWORD;
string BW_MESSAGING_APPLICATION_ID;
string BW_ACCOUNT_ID;
string BW_NUMBER;
string USER_NUMBER;
string Media = "https://cdn2.thecatapi.com/images/MTY3ODIyMQ.jpg";

try
{
    BW_USERNAME = Environment.GetEnvironmentVariable("BW_USERNAME");
    BW_PASSWORD = Environment.GetEnvironmentVariable("BW_PASSWORD");
    BW_MESSAGING_APPLICATION_ID = Environment.GetEnvironmentVariable("BW_MESSAGING_APPLICATION_ID");
    BW_ACCOUNT_ID = Environment.GetEnvironmentVariable("BW_ACCOUNT_ID");
    BW_NUMBER = Environment.GetEnvironmentVariable("BW_NUMBER");
    USER_NUMBER = Environment.GetEnvironmentVariable("USER_NUMBER");
}
catch (Exception)
{
    Console.WriteLine("Please set the environmental variables defined in the README");
    Environment.Exit(-1);
    throw;
}

Configuration configuration = new Configuration();
configuration.Username = BW_USERNAME;
configuration.Password = BW_PASSWORD;

app.MapPost("/sendMessages", async (HttpContext context) =>
    {
        var requestBody = new Dictionary<string, string>();
        using(var streamReader = new StreamReader(context.Request.Body))
        {
            var body = await streamReader.ReadToEndAsync();
            requestBody = JsonConvert.DeserializeObject<Dictionary<string,string>>(body);
        }

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

    var type = (string)((dynamic)requestBody[0]).type;
    
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
    
    var type = (string)((dynamic)requestBody[0]).type;
    
    if(type.Equals("message-received"))
    {
        var to = (string)((dynamic)requestBody[0]).message.to[0];
        var from = (string)((dynamic)requestBody[0]).message.from;
        var text = (string)((dynamic)requestBody[0]).message.text;

        var mediaApi = new MediaApi(configuration);

        foreach ( string item in (JArray)((dynamic)requestBody[0]).message.media)
        {
            string mediaId = item.Split(new string[] { "media/" }, StringSplitOptions.None).Last(); // gets the media ID used for GET media
            string[] mediaParts = mediaId.Split('/');
            string mediaName = mediaParts[mediaParts.Length - 1]; // gets the name of the downloaded media file
            
            if(!mediaName.Contains(".xml"))
            {
                var mediaFile = mediaApi.GetMedia(BW_ACCOUNT_ID, mediaId);
                using (var fileStream = File.Create(mediaName))
                {
                    mediaFile.CopyTo(fileStream);
                }
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
