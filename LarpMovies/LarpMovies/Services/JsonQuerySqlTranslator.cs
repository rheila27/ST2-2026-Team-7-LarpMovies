using System.Globalization;
using LarpMovies.Models;

namespace LarpMovies.Services
{
    //готов обект-пакет (контейнер), в който преводачът
    //събира на едно място всичко необходимо за изпълнението на готовата заявка
    public sealed record SqlSelectCommand(
        string Sql,
        IReadOnlyDictionary<string, object> Parameters,
        string ReadableSql);

    //за преобразуване на JSONQuery-то от Gemini в SQL заявка (SELECT * FROM Movies WHERE)
    //за базата данни (таблицата Movies), както и за защита от SQL инжекции
    //логиката е:
    //Естествен език -> Gemini (JSONQuery) -> Преводач (SqlSelectCommand) -> SQLite БД
    // Всички колони се проверяват в бял списък (whitelist), а стойностите се предават САМО като параметри.
    public static class JsonQuerySqlTranslator
    {
        // Базовата заявка с JOIN за режисьор и жанр
        private const string BaseSelect = """
        SELECT m.Id, m.Title, m.DirectorId, m.GenreId, m.Year, m.Rating, m.Actors, m.Description,
               d.Name AS DirectorName, g.Name AS GenreName
        FROM Movies m
        LEFT JOIN Directors d ON m.DirectorId = d.Id
        LEFT JOIN Genres g ON m.GenreId = g.Id
        """;

        // Бял списък с позволените колони 
        private static readonly IReadOnlyDictionary<string, string> AllowedColumns =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = "m.Id",
                ["номер"] = "m.Id",

                ["title"] = "m.Title",
                ["заглавие"] = "m.Title",
                ["име"] = "m.Title",

                ["year"] = "m.Year",
                ["година"] = "m.Year",

                ["rating"] = "m.Rating",
                ["рейтинг"] = "m.Rating",
                ["оценка"] = "m.Rating",

                ["directorname"] = "d.Name",
                ["director"] = "d.Name",
                ["режисьор"] = "d.Name",

                ["genrename"] = "g.Name",
                ["genre"] = "g.Name",
                ["жанр"] = "g.Name",

                ["actors"] = "m.Actors",
                ["актьори"] = "m.Actors",

                ["description"] = "m.Description",
                ["описание"] = "m.Description"
            };

        public static SqlSelectCommand Translate(JSONQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);

            var logicalOp = NormalizeLogicalOperator(query.LogicalOperator);
            var orderByColumn = ResolveOrderByColumn(query.OrderBy);
            var sortDir = NormalizeSortDirection(query.SortDirection);

            //Подготовка на списъците (кутийките за сглобяване)
            var whereParts = new List<string>();
            var readableWhereParts = new List<string>();
            var parameters = new Dictionary<string, object>();

            var conditions = query.Conditions ?? new List<QueryCondition>();
            var paramIndex = 0;

            //обхождане на условията и създаване на WHERE частта от SQL заявката
            foreach (var condition in conditions)
            {
                if (condition is null || string.IsNullOrWhiteSpace(condition.Field) || string.IsNullOrWhiteSpace(condition.Value))
                {
                    continue;
                }

                var sqlColumn = ResolveColumn(condition.Field);
                var op = (condition.Operator ?? "contains").Trim().ToLowerInvariant();
                var rawValue = condition.Value.Trim();
                var paramName = $"$p{paramIndex++}";

                // Обработка на цели числа (Id, Year)
                if (sqlColumn is "m.Id" or "m.Year")
                {
                    if (!int.TryParse(rawValue, out var intValue))
                    {
                        throw new ArgumentException($"Стойността за '{condition.Field}' трябва да е цяло число, а е подадено: '{rawValue}'.");
                    }

                    var sqlOp = ResolveNumericOperator(op);
                    whereParts.Add($"{sqlColumn} {sqlOp} {paramName}");
                    readableWhereParts.Add($"{sqlColumn} {sqlOp} {intValue}");
                    parameters[paramName] = intValue;
                }
                // Обработка на дробно число (Rating)
                else if (sqlColumn == "m.Rating")
                {
                    var normalizedRating = rawValue.Replace(',', '.');
                    if (!double.TryParse(normalizedRating, NumberStyles.Float, CultureInfo.InvariantCulture, out var ratingValue))
                    {
                        throw new ArgumentException($"Стойността за Rating трябва да е число, а е подадено: '{rawValue}'.");
                    }

                    var sqlOp = ResolveNumericOperator(op);
                    whereParts.Add($"{sqlColumn} {sqlOp} {paramName}");
                    readableWhereParts.Add($"{sqlColumn} {sqlOp} {ratingValue.ToString("0.0", CultureInfo.InvariantCulture)}");
                    parameters[paramName] = ratingValue;
                }
                // Текстови колони (Title, DirectorName, GenreName, Actors, Description)
                else
                {
                    var (clause, readableClause, paramValue) = BuildTextCondition(sqlColumn, op, rawValue, paramName);
                    whereParts.Add(clause);
                    readableWhereParts.Add(readableClause);
                    parameters[paramName] = paramValue;
                }
            }

            //Сглобяване на финалната SQL заявка
            var whereSql = whereParts.Count > 0 ? " WHERE " + string.Join($" {logicalOp} ", whereParts) : string.Empty;
            var readableWhereSql = readableWhereParts.Count > 0 ? " WHERE " + string.Join($" {logicalOp} ", readableWhereParts) : string.Empty;

            var orderSql = $" ORDER BY {orderByColumn} {sortDir}";
            var limitSql = string.Empty;
            var readableLimitSql = string.Empty;

            if (query.Limit is > 0)
            {
                var clampedLimit = Math.Clamp(query.Limit.Value, 1, 1000);
                limitSql = " LIMIT $limit";
                readableLimitSql = $" LIMIT {clampedLimit}";
                parameters["$limit"] = clampedLimit;
            }

            return new SqlSelectCommand(
                Sql: BaseSelect + whereSql + orderSql + limitSql,
                Parameters: parameters,
                ReadableSql: BaseSelect + readableWhereSql + orderSql + readableLimitSql);
        }


        //текстови филтри; подава се реалният SQL израз ($р0), израз с директно заместена стойност за UI,
        //точният параметър, който се подава. 
        private static (string Clause, string ReadableClause, string ParamValue) BuildTextCondition(
            string column, string op, string rawValue, string paramName)
        {
            return op switch
            {
                "contains" or "like" => (
                    $"LOWER({column}) LIKE LOWER({paramName})",
                    $"LOWER({column}) LIKE LOWER('%{EscapeSqlLiteral(rawValue)}%')",
                    $"%{rawValue}%"),

                "equals" or "eq" or "=" => (
                    $"LOWER({column}) = LOWER({paramName})",
                    $"LOWER({column}) = LOWER('{EscapeSqlLiteral(rawValue)}')",
                    rawValue),

                "startswith" => (
                    $"LOWER({column}) LIKE LOWER({paramName})",
                    $"LOWER({column}) LIKE LOWER('{EscapeSqlLiteral(rawValue)}%')",
                    $"{rawValue}%"),

                "endswith" => (
                    $"LOWER({column}) LIKE LOWER({paramName})",
                    $"LOWER({column}) LIKE LOWER('%{EscapeSqlLiteral(rawValue)}')",
                    $"%{rawValue}"),

                "notequals" or "neq" or "!=" => (
                    $"({column} IS NULL OR LOWER({column}) <> LOWER({paramName}))",
                    $"({column} IS NULL OR LOWER({column}) <> LOWER('{EscapeSqlLiteral(rawValue)}'))",
                    rawValue),

                _ => throw new ArgumentException($"Неподдържан текстов оператор: '{op}'.")
            };
        }

        //resolve функциите са за филтър и проверка на операторите и колоните, за да се гарантира,
        //че няма SQL инжекции и че се използват само разрешени колони и оператори.
        private static string ResolveNumericOperator(string op) => op switch
        {
            "equals" or "eq" or "=" or "contains" => "=",
            "notequals" or "neq" or "!=" => "<>",
            "gt" or ">" => ">",
            "lt" or "<" => "<",
            "gte" or ">=" => ">=",
            "lte" or "<=" => "<=",
            _ => throw new ArgumentException($"Неподдържан числов оператор: '{op}'.")
        };

        private static string ResolveColumn(string field)
        {
            var trimmed = field.Trim();
            if (AllowedColumns.TryGetValue(trimmed, out var column))
            {
                return column;
            }
            throw new ArgumentException($"Невалидно или непозволено поле: '{field}'.");
        }

        private static string ResolveOrderByColumn(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy)) return "m.Title";
            return ResolveColumn(orderBy);
        }

        //normalize функциите уеднаквяват формата на данните и да задават стойности по подразбиране
        //когато има неточности в промпта, дали от Gemini или потребителя
        private static string NormalizeLogicalOperator(string? logicalOperator)
        {
            if (string.IsNullOrWhiteSpace(logicalOperator)) return "AND";
            var norm = logicalOperator.Trim().ToUpperInvariant();
            return norm switch
            {
                "AND" or "И" => "AND",
                "OR" or "ИЛИ" => "OR",
                _ => throw new ArgumentException($"Невалиден логически оператор: '{logicalOperator}'.")
            };
        }

        private static string NormalizeSortDirection(string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortDirection)) return "ASC";
            var norm = sortDirection.Trim().ToUpperInvariant();
            return norm switch
            {
                "ASC" or "ASCENDING" => "ASC",
                "DESC" or "DESCENDING" => "DESC",
                _ => throw new ArgumentException($"Невалидна посока на сортиране: '{sortDirection}'.")
            };
        }

        //удвоява всяка единична кавичка, за да не се счупи заявката
        private static string EscapeSqlLiteral(string value) => value.Replace("'", "''");
    }
}
