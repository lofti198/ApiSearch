using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace ApiSearchConsole.Services.AI
{

    public class OpenAICompletionService : IChatCompletionsService
    {
        private readonly HttpClient _httpClient;

        public OpenAICompletionService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> GetChatCompletionsAsync(string userMessage, string? instruction, object jsonSchema, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(userMessage))
            {
                throw new ArgumentException("User message cannot be null or empty.", nameof(userMessage));
            }


            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

                List<dynamic> messages = new List<dynamic>() {
                    new { role = "system", content = instruction },
                    new { role = "user", content = userMessage }
                };

                var payload = new
                {
                    model = "gpt-4o",
                    messages,
                    response_format = new
                    {
                        type = "json_schema",
                        json_schema = jsonSchema
                    }
                };

                //var payload = new Dictionary<string, object>
                //{
                //    model = "gpt-4",
                //    messages = messages,
                //    response_format = new
                //    {
                //        type = "json_schema",
                //        json_schema = jsonSchema
                //    }
                //};

                //if (jsonSchema != null)
                //{
                //    payload["response_format"] = new
                //    {
                //        type = "json_schema",
                //        json_schema = jsonSchema
                //    };
                //}


                request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.SendAsync(request, ct);

                var responseContent = await httpResponse.Content.ReadAsStringAsync(ct);
                
                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Error from OpenAI Chat Completions: {httpResponse.ReasonPhrase}");
                }
                var chatResponseJson = JsonConvert.DeserializeObject<dynamic>(responseContent);

                // Extract JSON response from OpenAI's reply
                var responseText = chatResponseJson?.choices[0].message.content.ToString().Trim();

                //responseText = Regex.Replace(responseText, "^.*?{", "{", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                //responseText = Regex.Replace(responseText, "}.*?$", "}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                responseText = responseText.Trim();

                return responseText;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating chat response: {ex.Message}");
            }
        }
    }
}