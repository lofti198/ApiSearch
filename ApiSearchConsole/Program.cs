using Microsoft.Extensions.Configuration;
using ApiSearchConsole.Services;
using ApiSearchConsole.Services.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ApiSearchConsole;
using Plamar.QueueProcFacilities.Services.Caching;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<UrlScraperService>();
builder.Services.AddSingleton(sp => new CacheService("Cache"));
builder.Services.AddSingleton<IChatCompletionsService, OpenAICompletionService>();
builder.Services.AddSingleton<LoggerService>();
builder.Services.AddSingleton<UrlProcessingService>(); // Регистрация нового сервиса
builder.Services.AddSingleton<ICachingService<string, string>>(sp =>
{
    var cacheService = sp.GetRequiredService<CacheService>();
    return new FileStringCache("apisearcher","","",true);
});
var app = builder.Build();

var environment = app.Services.GetRequiredService<IHostEnvironment>();

var configurationBuilder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

var configuration = configurationBuilder.Build();

string urlToParse = configuration["urlToParse"] ?? throw new ArgumentNullException(nameof(urlToParse), "Cannot find parameter");
int depthToParse = int.Parse(configuration["depthToParse"] ?? throw new ArgumentNullException(nameof(urlToParse), "Cannot find parameter"));
int maxUrlsToProcess = configuration.GetValue<int>("maxUrlsToProcess", 0); // 0 = unlimited
int maxRelevantResults = configuration.GetValue<int>("maxRelevantResults", 0); // 0 = unlimited
int openAiIntervalInMilliseconds = configuration.GetValue<int>("openAiIntervalInMilliseconds", 1000); 

var prompt = configuration["prompt"];
var instruction = configuration["instruction"];
var jsonSchema = new { summary = "" };

var logger = app.Services.GetRequiredService<LoggerService>();
var urlProcessingService = app.Services.GetRequiredService<UrlProcessingService>();

var processedUrls = new HashSet<string>();
var relevantResults = new List<string>();
var queue = new Queue<(string Url, int Depth)>();

queue.Enqueue((urlToParse, 0));

logger.Log("🚀 Starting smart recursive scraping...");

await urlProcessingService.ProcessUrlsAsync(queue,
    processedUrls,
    relevantResults,
    depthToParse,
    maxUrlsToProcess,
    maxRelevantResults,
    instruction,
    prompt,
    jsonSchema,
    openAiIntervalInMilliseconds);

File.WriteAllText("RelevantResults.txt", string.Join("\n\n---\n\n", relevantResults));
logger.Log($"🏁 Process finished. {relevantResults.Count} relevant results saved to RelevantResults.txt");
