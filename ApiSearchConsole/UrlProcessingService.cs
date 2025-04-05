using ApiSearchConsole.Services.AI;
using ApiSearchConsole.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using Plamar.QueueProcFacilities.Services.Caching;

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
            object jsonSchema,
            int openAiIntervalInMilliseconds)
        {
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
                    //If you make too many requests, you may receive a 429 error )))))
                    Thread.Sleep(openAiIntervalInMilliseconds);
                    
                    var cachedResult = await llmCachingService.GetCache(instruction + textContent);

                    var result = "";
                    if(!String.IsNullOrEmpty(cachedResult))result = cachedResult;
                    else
                    {
                        result = await openAiService.GetChatCompletionsAsync(textContent, instruction, jsonSchema);
                        if (!string.IsNullOrWhiteSpace(result))
                            llmCachingService.SaveCache(instruction + textContent, result);
                    }

                    await openAiService.GetChatCompletionsAsync(textContent, instruction, jsonSchema);
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
        }
    }
}
