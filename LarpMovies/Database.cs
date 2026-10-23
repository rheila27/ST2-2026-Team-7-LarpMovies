using LarpMovies.Models;
using Microsoft.Data.Sqlite;

namespace LarpMovies.Data;

public sealed class Database : IDatabase
{
    private static readonly object Lock = new();
    private static Database? _instance;
    private readonly string _connectionString;

    // private konstruktor: otvun ne moze da se pishe new Database - singleton
    private Database(string connectionString)
    {
        _connectionString = connectionString;
        EnsureCreated();
    }

    public static Database Instance =>
        _instance ?? throw new InvalidOperationException(
            "Database ne e inicializirana. Izvikaite Database.Initialize(connectionString).");

    // suzdava edinstveniq instance pri purvo izvikvane, posle vrushta sushtiya
    public static Database Initialize(string connectionString)
    {
        if (_instance is null)
        {
            lock (Lock)
            {
                _instance ??= new Database(connectionString);
            }
        }
        return _instance;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void EnsureCreated()
    {
        var builder = new SqliteConnectionStringBuilder(_connectionString);
        var dir = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var isNewFile = !File.Exists(builder.DataSource);

        using var connection = Open();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Movies (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Director TEXT NOT NULL,
                    Year INTEGER NOT NULL,
                    Genre TEXT NOT NULL,
                    Rating REAL NOT NULL,
                    Description TEXT NOT NULL DEFAULT '',
                    Actors TEXT NOT NULL DEFAULT ''
                );
                """;
            cmd.ExecuteNonQuery();
        }

        if (isNewFile)
        {
            Seed(connection);
        }
    }

    // nqkolko primerni filma
    private static void Seed(SqliteConnection connection)
    {
        var samples = new (string Title, string Director, int Year, string Genre, double Rating, string Description, string Actors)[]
        {
            ("Dune", "Denis Villeneuve", 2021, "Sci-Fi", 8.0,
                "A young noble takes control of a desert planet and its precious spice.",
                "Timothee Chalamet, Zendaya, Rebecca Ferguson"),
            ("Inception", "Christopher Nolan", 2010, "Sci-Fi", 8.8,
                "A thief enters dreams to plant an idea in a target's mind.",
                "Leonardo DiCaprio, Joseph Gordon-Levitt, Elliot Page"),
            ("The Godfather", "Francis Ford Coppola", 1972, "Crime", 9.2,
                "The aging head of a crime family hands control to his reluctant son.",
                "Marlon Brando, Al Pacino, James Caan"),
            ("Parasite", "Bong Joon-ho", 2019, "Thriller", 8.5,
                "A poor family schemes its way into the lives of a wealthy household.",
                "Song Kang-ho, Choi Woo-shik, Park So-dam"),
            ("Spirited Away", "Hayao Miyazaki", 2001, "Animation", 8.6,
                "A girl wanders into a world of spirits and must work to free her parents.",
                "Rumi Hiiragi, Miyu Irino, Mari Natsuki"),
            ("Pulp Fiction", "Quentin Tarantino", 1994, "Crime", 8.9,
                "Intertwined stories of hitmen, a boxer and a gangster's wife in Los Angeles.",
                "John Travolta, Samuel L. Jackson, Uma Thurman")
        };

        using var tx = connection.BeginTransaction();
        foreach (var m in samples)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                "INSERT INTO Movies (Title, Director, Year, Genre, Rating, Description, Actors) " +
                "VALUES ($title, $director, $year, $genre, $rating, $description, $actors)";
            cmd.Parameters.AddWithValue("$title", m.Title);
            cmd.Parameters.AddWithValue("$director", m.Director);
            cmd.Parameters.AddWithValue("$year", m.Year);
            cmd.Parameters.AddWithValue("$genre", m.Genre);
            cmd.Parameters.AddWithValue("$rating", m.Rating);
            cmd.Parameters.AddWithValue("$description", m.Description);
            cmd.Parameters.AddWithValue("$actors", m.Actors);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    // prevrushta red ot bazata v obekt Movie
    private static Movie Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Title = r.GetString(1),
        Director = r.GetString(2),
        Year = r.GetInt32(3),
        Genre = r.GetString(4),
        Rating = r.GetDouble(5),
        Description = r.GetString(6),
        Actors = r.GetString(7)
    };

    // Select
    public IReadOnlyList<Movie> GetAll()
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Title, Director, Year, Genre, Rating, Description, Actors FROM Movies ORDER BY Title COLLATE NOCASE";
        using var reader = cmd.ExecuteReader();
        var result = new List<Movie>();
        while (reader.Read())
        {
            result.Add(Map(reader));
        }
        return result;
    }

    // Insert
    public int Add(Movie movie)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Movies (Title, Director, Year, Genre, Rating, Description, Actors)
            VALUES ($title, $director, $year, $genre, $rating, $description, $actors);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$title", movie.Title.Trim());
        cmd.Parameters.AddWithValue("$director", movie.Director.Trim());
        cmd.Parameters.AddWithValue("$year", movie.Year);
        cmd.Parameters.AddWithValue("$genre", movie.Genre.Trim());
        cmd.Parameters.AddWithValue("$rating", movie.Rating);
        cmd.Parameters.AddWithValue("$description", (movie.Description ?? "").Trim());
        cmd.Parameters.AddWithValue("$actors", (movie.Actors ?? "").Trim());
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}