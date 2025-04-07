using ApiSearchConsole.Services;
using ApiSearchConsole.Services.AI;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Plamar.QueueProcFacilities.Services.Caching;
using System.Text.RegularExpressions;

namespace ApiSearchConsole
{
    public class UrlProcessingService(
        UrlScraperService scraperService,
        CacheService cacheService,
        OpenAICompletionService openAiService,
        LoggerService logger,
        ICachingService<string, string> llmCachingService)
    {
        public async Task ProcessUrlsAsync(
            Queue<(string Url, int Depth)> queue,
            HashSet<string> processedUrls,
            List<string> relevantResults,
            int depthToParse,
            int maxUrlsToProcess,
            int maxRelevantResults,
            string? instruction,
            object? _, // previous jsonSchema param not needed
            int openAiIntervalInMilliseconds)
        {
            var jsonSchema = JsonSchemeGenerator.GetJsonSchema(); // 👈 Injected here

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

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(content);
                var textContent = Regex.Replace(htmlDoc.DocumentNode.InnerText, @"\s+", " ").Trim();

                try
                {
                    Thread.Sleep(openAiIntervalInMilliseconds);

                    var cacheKey = instruction + textContent;
                    var cachedResult = await llmCachingService.GetCache(cacheKey);

                    string result;
                    if (!string.IsNullOrEmpty(cachedResult))
                    {
                        result = cachedResult;
                    }
                    else
                    {
                        result = await openAiService.GetChatCompletionsAsync(textContent, instruction, jsonSchema);
                        if (!string.IsNullOrWhiteSpace(result))
                            llmCachingService.SaveCache(cacheKey, result);
                    }

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        var response = JsonConvert.DeserializeObject<AnswerResponse>(result);
                        var answers = response?.answers;

                        if (answers != null && answers.Any())
                        {
                            logger.Log($"✅ Relevant content found at: {url}");
                            foreach (var a in answers)
                            {
                                Console.WriteLine($"🟢 Answer found: {a.originalQuestion} → {a.answer}");
                            }

                            relevantResults.Add($"URL: {url}\n{result}");
                        }
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
        }
        public class AnswerResponse
        {
            public List<AnswerItem> answers { get; set; } = new();
        }

        public class AnswerItem
        {
            public string originalQuestion { get; set; } = "";
            public string answer { get; set; } = "";
        }
    }
}
