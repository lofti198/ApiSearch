using ApiSearch.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<GeminiFlashService>();
builder.Services.AddSingleton<UrlScraperService>();
builder.Services.AddSingleton<IStringCapitalizationService, StringCapitalizationService>(); // Register the service with an interface

var app = builder.Build();

var geminiService = app.Services.GetRequiredService<GeminiFlashService>();
var urlScraperService = app.Services.GetRequiredService<UrlScraperService>();
var stringCapitalizationService = app.Services.GetRequiredService<StringCapitalizationService>(); // Resolve the new service

// Example usage
string prompt = "Your prompt here";
string capitalizedPrompt = stringCapitalizationService.Capitalize(prompt); // Capitalize the prompt

string result = await geminiService.ProcessPromptAsync(capitalizedPrompt);
Console.WriteLine($"Response: {result}");

// Scrape URLs
await urlScraperService.ScrapeAsync();

app.Run();