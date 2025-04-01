using Microsoft.Extensions.Configuration;
using ApiSearchConsole.Services;
using ApiSearchConsole.Services.AI;
using Microsoft.Extensions.DependencyInjection;

//NOTE: maybe Webapplication is not the best choice for this console app
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<UrlScraperService>();
builder.Services.AddSingleton(sp => new CacheService("Cache"));
builder.Services.AddSingleton<OpenAICompletionService>();
builder.Services.AddSingleton<LoggerService>();

var app = builder.Build();

// TODO: get path where the mainmodule exe is located
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string urlToParse = configuration["urlToParse"];
int depthToParse = int.Parse(configuration["depthToParse"]);
int maxUrlsToProcess = configuration.GetValue<int>("maxUrlsToProcess", 0); // 0 = unlimited
int maxRelevantResults = configuration.GetValue<int>("maxRelevantResults", 0); // 0 = unlimited

var prompt = configuration["prompt"];
var instruction = configuration["instruction"];
var jsonSchema = new { summary = "" };

var scraperService = app.Services.GetRequiredService<UrlScraperService>();
var cacheService = app.Services.GetRequiredService<CacheService>();
var openAiService = app.Services.GetRequiredService<OpenAICompletionService>();
var logger = app.Services.GetRequiredService<LoggerService>();

var processedUrls = new HashSet<string>();
var relevantResults = new List<string>();
var queue = new Queue<(string Url, int Depth)>();

queue.Enqueue((urlToParse, 0));

logger.Log("🚀 Starting smart recursive scraping...");

//TODO: move to separate service
while (queue.Count > 0 &&
      (maxUrlsToProcess == 0 || processedUrls.Count < maxUrlsToProcess) &&
      (maxRelevantResults == 0 || relevantResults.Count < maxRelevantResults))
{
    var (url, currentDepth) = queue.Dequeue();
    if (processedUrls.Contains(url) || currentDepth > depthToParse)
        continue;

    logger.Log($"🌐 Processing: {url}");
    processedUrls.Add(url);

    string content;
    if (cacheService.IsCached(url))
    {
        content = cacheService.LoadFromCache(url);
    }
    else
    {
        try
        {
            content = await new HttpClient().GetStringAsync(url);
            cacheService.SaveToCache(url, content);
        }
        catch (Exception ex)
        {
            logger.Log($"❌ Failed to load {url}: {ex.Message}");
            continue;
        }
    }

    try
    {
        var result = await openAiService.GetChatCompletionsAsync(content, instruction, jsonSchema);
        if (!string.IsNullOrWhiteSpace(result))
        {
            logger.Log($"✅ Relevant content found at: {url}");
            relevantResults.Add($"URL: {url}\n{result}");
        }
    }
    catch (Exception ex)
    {
        logger.Log($"⚠️ OpenAI failed on {url}: {ex.Message}");
    }

    if (currentDepth < depthToParse)
    {
        var links = scraperService.ExtractLinks(content);
        foreach (var link in links)
        {
            try
            {
                var absolute = new Uri(new Uri(url), link).ToString();
                if (!processedUrls.Contains(absolute))
                    queue.Enqueue((absolute, currentDepth + 1));
            }
            catch { /* Skip malformed links */ }
        }
    }
}

File.WriteAllText("RelevantResults.txt", string.Join("\n\n---\n\n", relevantResults));
logger.Log($"🏁 Process finished. {relevantResults.Count} relevant results saved to RelevantResults.txt");
