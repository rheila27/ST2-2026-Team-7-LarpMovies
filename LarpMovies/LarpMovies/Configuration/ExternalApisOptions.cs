namespace LarpMovies.Configuration
{
    // Настройки за външните услуги. Свързва се със секцията "ExternalApis" в appsettings.json.
    public sealed class ExternalApisOptions
    {
        public const string SectionName = "ExternalApis";

        // Google Gemini API ключ за преобразуване на заявка на естествен език в JSONQuery обект.
        //чете се автоматично от appsettings.json
        public string? GeminiApiKey { get; set; }

        // Идентификатор на модела Gemini (по подразбиране gemini-3-flash-preview).
        public string GeminiModel { get; set; } = "gemini-3-flash-preview";
    }
}
