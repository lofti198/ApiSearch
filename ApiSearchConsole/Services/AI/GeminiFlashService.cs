// using System;
// using System.Net.Http;
// using System.Text;
// using System.Text.Json;
// using System.Threading.Tasks;

// namespace ApiSearch.Services
// {
//     public class GeminiFlashService : IChatCompletionsService
//     {
//         private readonly HttpClient _httpClient;
//         private readonly string _apiKey;

//         public GeminiFlashService(HttpClient httpClient)
//         {
//             _httpClient = httpClient;
//             _apiKey = Environment.GetEnvironmentVariable("GEMINI_2_FLASH_APIKEY") 
//                       ?? throw new InvalidOperationException("GEMINI_2_FLASH_APIKEY is not set in environment variables.");
//         }

//         public async Task<string> (string userMessage, string instruction, object jsonSchema, 
//         CancellationToken ct = default)
//         {
//             var requestBody = new
//             {
//                 prompt = userMessage
//             };

//             var requestContent = new StringContent(
//                 JsonSerializer.Serialize(requestBody),
//                 Encoding.UTF8,
//                 "application/json"
//             );

//             var request = new HttpRequestMessage(HttpMethod.Post, "https://api.gemini2flash.com/process")
//             {
//                 Headers = { { "Authorization", $"Bearer {_apiKey}" } },
//                 Content = requestContent
//             };

//             var response = await _httpClient.SendAsync(request);

//             if (!response.IsSuccessStatusCode)
//             {
//                 throw new Exception($"Error processing prompt: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
//             }

//             return await response.Content.ReadAsStringAsync();
//         }
//     }
// }
