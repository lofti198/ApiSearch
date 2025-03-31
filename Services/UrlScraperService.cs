using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ApiSearch.Services
{
    public class UrlScraperService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public UrlScraperService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task ScrapeAsync()
        {
            string startUrl = _configuration["Scraper:StartUrl"];
            int maxDepth = int.Parse(_configuration["Scraper:MaxDepth"]);

            if (string.IsNullOrEmpty(startUrl) || maxDepth <= 0)
            {
                Console.WriteLine("Invalid configuration for URL scraper.");
                return;
            }

            await ScrapeUrlAsync(startUrl, maxDepth, 0);
        }

        private async Task ScrapeUrlAsync(string url, int maxDepth, int currentDepth)
        {
            if (currentDepth >= maxDepth) return;

            try
            {
                Console.WriteLine($"Scraping URL: {url}");
                var response = await _httpClient.GetStringAsync(url);

                // Extract links using a simple regex
                var links = Regex.Matches(response, @"href=""(http[s]?://[^""]+)""")
                                  .Select(m => m.Groups[1].Value)
                                  .Distinct();

                foreach (var link in links)
                {
                    await ScrapeUrlAsync(link, maxDepth, currentDepth + 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping URL {url}: {ex.Message}");
            }
        }
    }
}
