using System.Text;
using System.Text.Json;

namespace Backend.Services
{
    public class GeminiContentService : IAiContentService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GeminiContentService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<bool> IsContentSafeAsync(string content)
        {
            // Am sters metoda de listare (ListAvailableModels) pentru ca acum stim modelul.

            var apiKey = _configuration["GoogleAI:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                return !IsLocalCheckBad(content);
            }

            // --- CONFIGURARE PENTRU GEMMA 3 (din lista ta) ---
            // Folosim exact numele din lista ta: models/gemma-3-27b-it
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemma-3-27b-it:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { 
                                // Gemma preferă instrucțiuni clare și directe.
                                text = $"Analyze this text for hate speech, violence, or severe insults. Reply with only one word: 'SAFE' or 'UNSAFE'. Text: \"{content}\""
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0 // Vrem răspuns fix, nu creativ
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    // Dacă modelul e prea ocupat sau dă eroare, vedem exact ce zice Google
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gemma API Error: {response.StatusCode} - {errorMsg}");
                    return true; // Fail-open (nu blocăm userul la erori tehnice)
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                // Parsare răspuns
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];

                    // Verificăm dacă Gemma a refuzat să răspundă din motive de siguranță (Safety Filters)
                    if (firstCandidate.TryGetProperty("finishReason", out var finishReason) &&
                        finishReason.GetString() == "SAFETY")
                    {
                        Console.WriteLine("Gemma blocked content due to safety filters.");
                        return false; // Conținut nesigur
                    }

                    // Extragem textul răspunsului
                    if (firstCandidate.TryGetProperty("content", out var contentElement) &&
                        contentElement.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var aiTextResponse = parts[0].GetProperty("text").GetString();

                        // Curățăm răspunsul (Gemma poate pune spații sau puncte)
                        bool isUnsafe = aiTextResponse!.Trim().ToUpper().Contains("UNSAFE");
                        return !isUnsafe;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemma Exception: {ex.Message}");
                return true;
            }
        }

        private bool IsLocalCheckBad(string text)
        {
            var badWords = new[] { "prost", "idiot", "ura", "moarte", "discriminare" };
            return badWords.Any(w => text.ToLower().Contains(w));
        }
    }
}