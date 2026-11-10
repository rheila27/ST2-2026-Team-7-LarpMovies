using System.Net;
using System.Text;
using System.Text.Json;
using LarpMovies.Configuration;
using LarpMovies.Models;
using Microsoft.Extensions.Options;

namespace LarpMovies.Services
{
    public sealed class GeminiQueryService : IGeminiQueryService
    {
        //определя към кой интернет адрес на Google се изпраща HTTP заявката
        private const string GeminiEndpointTemplate =
            "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

        private const string DefaultModel = "gemini-3-flash-preview";

        //верига от резервни модели при надвишен лимит (HTTP 429) или претоварване (HTTP 503)
        private static readonly string[] FallbackModels =
        {
        "gemini-3-flash-preview",
        "gemini-3.5-flash",
        "gemini-2.5-flash",
        "gemini-2.5-flash-lite"
        };

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        //за dependency injection при конструктора
        private readonly HttpClient _httpClient;
        private readonly ExternalApisOptions _options;
        private readonly ILogger<GeminiQueryService> _logger;

        public GeminiQueryService(
            HttpClient httpClient,
            IOptions<ExternalApisOptions> options,
            ILogger<GeminiQueryService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        //Проверява за промпт и дали има зададен api ключ в appsettings.json
        public async Task<JSONQuery> TranslatePromptToJsonQueryAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Промптът не може да бъде празен.", nameof(prompt));
            }

            var apiKey = _options.GeminiApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Gemini API ключът липсва. Моля, добавете го в appsettings.json в секция 'ExternalApis:GeminiApiKey'.");
            }

            var configuredModel = string.IsNullOrWhiteSpace(_options.GeminiModel)
                ? DefaultModel
                : _options.GeminiModel.Trim();

            // Списък от модели за опит (първо избраният, последван от резервните без повторения)
            var modelsToTry = new List<string> { configuredModel };
            foreach (var m in FallbackModels)
            {
                if (!modelsToTry.Contains(m, StringComparer.OrdinalIgnoreCase))
                {
                    modelsToTry.Add(m);
                }
            }

            var requestBody = BuildRequestBody(prompt);
            var jsonContent = JsonSerializer.Serialize(requestBody);

            Exception? lastException = null;

            //обхождане на списъка с ai модели
            foreach (var model in modelsToTry)
            {
                var url = string.Format(GeminiEndpointTemplate, Uri.EscapeDataString(model), Uri.EscapeDataString(apiKey));
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                try
                {
                    using var response = await _httpClient.SendAsync(request, cancellationToken);

                    if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                    {
                        _logger.LogWarning("Модел {Model} върна {StatusCode}. Пробваме следващ резервен модел...", model, response.StatusCode);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        throw new HttpRequestException(
                            $"Грешка при заявка към Gemini API ({model}): {response.StatusCode} - {errBody}");
                    }

                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    return ParseGeminiResponse(responseJson);
                }
                catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                {
                    _logger.LogWarning("Модел {Model} върна квотен лимит. Пробваме следващ модел...", model);
                    lastException = ex;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(ex, "Грешка при комуникация с модел {Model}.", model);
                    break;
                }
            }

            throw new InvalidOperationException(
                "Всички опитани Gemini модели надвишиха квотата си или са временно недостъпни. Моля, опитайте след малко.",
                lastException);
        }

        //сглобява точния JSON формат, който сървърите на Google изискват
        private static object BuildRequestBody(string userPrompt)
        {
            const string systemInstruction = """
            Ти си помощен асистент за база данни с филми.
            Твоята задача е да преобразуваш заявки на естествен език (български или английски) в JSONQuery обект.
            
            Полета в таблицата Movies:
            - Title (текст, заглавие на филма)
            - DirectorName (текст, име на режисьора)
            - GenreName (текст, име на жанра)
            - Year (цяло число, година на издаване)
            - Rating (дробно число между 0.0 и 10.0, оценка)
            - Actors (текст, актьорски състав)
            - Description (текст, описание/сюжет)

            Позволени оператори:
            - Текстови: contains, equals, startsWith, endsWith, notEquals
            - Числови (за Year и Rating): gt (>), gte (>=), lt (<), lte (<=), equals (=), notEquals (!=)

            Схема на JSON отговора:
            {
              "logicalOperator": "AND" или "OR",
              "conditions": [
                { "field": "DirectorName", "operator": "contains", "value": "Нолан" }
              ],
              "orderBy": "Title",
              "sortDirection": "ASC" или "DESC",
              "limit": цяло число или null,
              "explanation": "Кратко обяснение на български какво търси заявката."
            }
            Връщай ЕДИНСТВЕНО чист JSON валиден по тази схема.
            """;

            return new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents = new[]
                {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userPrompt } }
                }
            },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.1
                }
            };
        }

        //взима отговора от Gemini и го парсва в JSONQuery обект чрез десериализация
        private static JSONQuery ParseGeminiResponse(string rawResponseJson)
        {
            using var doc = JsonDocument.Parse(rawResponseJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Gemini не върна резултат (candidates е празен).");
            }

            var candidate = candidates[0];
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Невалидна структура на отговора от Gemini.");
            }

            var text = parts[0].GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Празен отговор от модела.");
            }

            var jsonQuery = JsonSerializer.Deserialize<JSONQuery>(text, JsonOpts);
            return jsonQuery ?? throw new InvalidOperationException("Неуспешно парсване на JSONQuery от модела.");
        }
    }
}
