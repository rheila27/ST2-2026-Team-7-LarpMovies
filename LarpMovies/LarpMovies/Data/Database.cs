using LarpMovies.Models;
using Microsoft.Data.Sqlite;

namespace LarpMovies.Data;

public sealed class Database : IDatabase
{
    private const string InsertSql =
        "INSERT INTO Movies (Title, Director, Year, Genre, Rating, Description, Actors) " +
        "VALUES ($title, $director, $year, $genre, $rating, $description, $actors)";

    private static readonly object Lock = new();
    private static Database? _instance;
    private readonly string _connectionString;

    // private konstruktor: otvun ne moze da se pishe new Database()
    private Database(string connectionString)
    {
        _connectionString = connectionString;
        EnsureCreated();
    }

    public static Database Instance =>
        _instance ?? throw new InvalidOperationException(
            "Database ne e inicializirana. Izvikaite Database.Initialize(connectionString).");

    // suzdava edinstveniq instance pri purvo izvikvane (thread-safe), posle vrushta sushtiya
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

    // nqkolko primerni filma, za da ima ot kakvo da filtrirate
    private static void Seed(SqliteConnection connection)
    {
        var samples = new Movie[]
        {
            new() { Title = "Dune", Director = "Denis Villeneuve", Year = 2021, Genre = "Sci-Fi", Rating = 8.0,
                    Description = "A young noble takes control of a desert planet and its precious spice.",
                    Actors = "Timothee Chalamet, Zendaya, Rebecca Ferguson" },
            new() { Title = "Inception", Director = "Christopher Nolan", Year = 2010, Genre = "Sci-Fi", Rating = 8.8,
                    Description = "A thief enters dreams to plant an idea in a target's mind.",
                    Actors = "Leonardo DiCaprio, Joseph Gordon-Levitt, Elliot Page" },
            new() { Title = "The Godfather", Director = "Francis Ford Coppola", Year = 1972, Genre = "Crime", Rating = 9.2,
                    Description = "The aging head of a crime family hands control to his reluctant son.",
                    Actors = "Marlon Brando, Al Pacino, James Caan" },
            new() { Title = "Parasite", Director = "Bong Joon-ho", Year = 2019, Genre = "Thriller", Rating = 8.5,
                    Description = "A poor family schemes its way into the lives of a wealthy household.",
                    Actors = "Song Kang-ho, Choi Woo-shik, Park So-dam" },
            new() { Title = "Spirited Away", Director = "Hayao Miyazaki", Year = 2001, Genre = "Animation", Rating = 8.6,
                    Description = "A girl wanders into a world of spirits and must work to free her parents.",
                    Actors = "Rumi Hiiragi, Miyu Irino, Mari Natsuki" },
            new() { Title = "Pulp Fiction", Director = "Quentin Tarantino", Year = 1994, Genre = "Crime", Rating = 8.9,
                    Description = "Intertwined stories of hitmen, a boxer and a gangster's wife in Los Angeles.",
                    Actors = "John Travolta, Samuel L. Jackson, Uma Thurman" }
        };

        using var tx = connection.BeginTransaction();
        foreach (var movie in samples)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = InsertSql;
            Bind(cmd, movie);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    // prevrushta edin red ot bazata v obekt Movie
    private static Movie Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Title = r.GetString(1),
        Director = r.GetString(2),
        Year = r.GetInt32(3),
        Genre = r.GetString(4),
        Rating = r.GetDouble(5),
        Description = r.IsDBNull(6) ? null : r.GetString(6),
        Actors = r.IsDBNull(7) ? null : r.GetString(7)
    };

    // slaga stoinostite na filma kato parametri (praznite neizbezhdani poleta stavat NULL)
    private static void Bind(SqliteCommand cmd, Movie m)
    {
        cmd.Parameters.AddWithValue("$title", m.Title.Trim());
        cmd.Parameters.AddWithValue("$director", m.Director.Trim());
        cmd.Parameters.AddWithValue("$year", m.Year);
        cmd.Parameters.AddWithValue("$genre", m.Genre.Trim());
        cmd.Parameters.AddWithValue("$rating", m.Rating);
        cmd.Parameters.AddWithValue("$description",
            string.IsNullOrWhiteSpace(m.Description) ? string.Empty : m.Description.Trim());
        cmd.Parameters.AddWithValue("$actors",
            string.IsNullOrWhiteSpace(m.Actors) ? string.Empty : m.Actors.Trim());
    }

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
        cmd.CommandText = InsertSql + "; SELECT last_insert_rowid();";
        Bind(cmd, movie);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}