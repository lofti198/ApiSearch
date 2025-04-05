using System.Net.Http;
using System.Text.RegularExpressions;

namespace ApiSearchConsole.Services
{
    public class UrlScraperService
    {
        private readonly HttpClient _httpClient;

        public UrlScraperService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> ScrapeAsync(string url, int depth, HashSet<string> visitedUrls = null)
        {
            visitedUrls ??= new HashSet<string>();
            if (depth == 0 || visitedUrls.Contains(url)) return new List<string>();

            visitedUrls.Add(url);
            var result = new List<string> { url };

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var links = ExtractLinks(response);

                foreach (var link in links)
                {
                    var absoluteLink = new Uri(new Uri(url), link).ToString();
                    result.AddRange(await ScrapeAsync(absoluteLink, depth - 1, visitedUrls));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping {url}: {ex.Message}");
            }

            return result;
        }

        //TODO: extract just internal links (from the same domain, however including subdomains)
        // subdomain.site.com, another.site.com, site.com
        public IEnumerable<string> ExtractLinks(string html)
        {
            var regex = new Regex(@"href\s*=\s*[""'](?<url>[^""']+)[""']", RegexOptions.IgnoreCase);
            var disallowedExtensions = new[]
            {
        ".css", ".js", ".png", ".jpg", ".jpeg", ".svg", ".ico",
        ".woff", ".woff2", ".ttf", ".eot", ".otf", ".pdf", ".zip",
        ".rar", ".exe", ".mp3", ".mp4", ".webm", ".avi", ".mov",".webmanifest"
    };

            return regex.Matches(html)
                .Select(m => m.Groups["url"].Value)
                .Where(link =>
                    !string.IsNullOrEmpty(link) &&
                    !disallowedExtensions.Any(ext => link.Contains('?') ?
                        link.Substring(0, link.IndexOf('?')).EndsWith(ext, StringComparison.OrdinalIgnoreCase) :
                        link.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
        }

    }
}