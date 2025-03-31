using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using ApiSearchConsole.Services;
using ApiSearchConsole.Services.AI;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddHttpClient();
builder.Services.AddSingleton<UrlScraperService>();
builder.Services.AddSingleton<CacheService>(sp => new CacheService("Cache"));
builder.Services.AddSingleton<OpenAICompletionService>(); // Use your existing OpenAICompletionService
builder.Services.AddSingleton<LoggerService>();

var app = builder.Build();

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string urlToParse = configuration["urlToParse"];
int depthToParse = int.Parse(configuration["depthToParse"]);

var scraperService = app.Services.GetRequiredService<UrlScraperService>();
var cacheService = app.Services.GetRequiredService<CacheService>();
var openAiService = app.Services.GetRequiredService<OpenAICompletionService>();
var logger = app.Services.GetRequiredService<LoggerService>();

logger.Log("Starting URL scraping...");
var urls = await scraperService.ScrapeAsync(urlToParse, depthToParse);

logger.Log("Caching scraped pages...");
foreach (var url in urls)
{
    if (!cacheService.IsCached(url))
    {
        try
        {
            var content = await new HttpClient().GetStringAsync(url);
            cacheService.SaveToCache(url, content);
        }
        catch (Exception ex)
        {
            logger.Log($"Failed to cache {url}: {ex.Message}");
        }
    }
}

logger.Log("Sending data to OpenAI...");
var prompt = "Summarize the following API documentation:\n" + string.Join("\n", urls);
var instruction = "You are an assistant that summarizes API documentation.";
var jsonSchema = new { summary = "" }; // Define the expected JSON schema for the response

var openAiResponse = await openAiService.GetChatCompletionsAsync(prompt, instruction, jsonSchema);

logger.Log("Saving OpenAI response...");
File.WriteAllText("OpenAiResponse.txt", openAiResponse);

logger.Log("Process completed.");