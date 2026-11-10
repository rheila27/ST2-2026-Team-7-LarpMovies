using LarpMovies.Models;

namespace LarpMovies.Services
{
    //Договор за услуга, която изпраща заявка на естествен език към Gemini API
    //и връща структуриран JSONQuery обект.
    public interface IGeminiQueryService
    {
        Task<JSONQuery> TranslatePromptToJsonQueryAsync(
            string prompt,
            CancellationToken cancellationToken = default);
    }
}
