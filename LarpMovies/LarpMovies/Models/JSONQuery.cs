using System.Text.Json.Serialization;

namespace LarpMovies.Models
{
    public sealed class JSONQuery
    {
        //Логически оператор за свързване на условията: "AND" (по подразбиране) или "OR".
        [JsonPropertyName("logicalOperator")]
        public string LogicalOperator { get; set; } = "AND";

        //Списък от условия за филтриране върху филмите (Title, Year, Rating, DirectorName, GenreName, Actors).</summary>
        [JsonPropertyName("conditions")]
        public List<QueryCondition> Conditions { get; set; } = new();

        //Колона за сортиране (по подразбиране Title).
        [JsonPropertyName("orderBy")]
        public string? OrderBy { get; set; } = "Title";

        //Посока на сортиране: "ASC" (възходящо) или "DESC" (низходящо).
        [JsonPropertyName("sortDirection")]
        public string? SortDirection { get; set; } = "ASC";

        //Максимален брой записи (LIMIT). Null означава без ограничение.
        [JsonPropertyName("limit")]
        public int? Limit { get; set; }

        /// <summary>Кратко обяснение на български език как моделът е интерпретирал заявката за филми.</summary>
        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }
    }

    // Единично условие за филтриране
    public sealed class QueryCondition
    {
        // Име на полето: Title, DirectorName, GenreName, Year, Rating, Actors, Description.
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        // Оператор за сравнение
        // contains, equals, startsWith, endsWith, notEquals, gt, lt, gte, lte.
        [JsonPropertyName("operator")]
        public string Operator { get; set; } = "contains";

        //Търсената стойност, която се подава като SQL параметър.
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
