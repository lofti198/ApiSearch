using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.Configuration.Json;

// ...existing code...

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string urlToParse = configuration["urlToParse"];
int depthToParse = int.Parse(configuration["depthToParse"]);

// Use the extracted parameters
Console.WriteLine($"URL to Parse: {urlToParse}");
Console.WriteLine($"Depth to Parse: {depthToParse}");

// ...existing code...