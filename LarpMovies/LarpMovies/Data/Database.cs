using LarpMovies.Models;
using LarpMovies.Services;
using Microsoft.Data.Sqlite;

namespace LarpMovies.Data;

public sealed class Database : IDatabase
{
    // 1) vmesto tekstove Director/Genre pazim samo klyuchove kum drugite tablici
    private const string InsertSql =
        "INSERT INTO Movies (Title, Year, Rating, Description, Actors, DirectorId, GenreId) " +
        "VALUES ($title, $year, $rating, $description, $actors, $directorId, $genreId)";

    // SELECT za filmite s JOIN, za da vzemem imenata na rejisyora i janra (redut na kolonite = indeksite v Map)
    private const string MovieSelect = """
        SELECT m.Id, m.Title, m.Year, m.Rating, m.Description, m.Actors,
               m.DirectorId, m.GenreId, d.Name, g.Name
        FROM Movies m
        JOIN Directors d ON d.Id = m.DirectorId
        JOIN Genres    g ON g.Id = m.GenreId
        """;

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

        // регистрация на unicode функции, за да не счита еднакви букви за различни
        // (подминава се case-sensitivity-то на sqlite)
        connection.CreateFunction("LOWER", (string? s) => s?.ToLowerInvariant());
        connection.CreateFunction("UPPER", (string? s) => s?.ToUpperInvariant());

        // SQLite po podrazbirane ignorira FOREIGN KEY; nastroikata e za vsyaka vrazka otdelno
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

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

        // dev-migraciya: ako Movies e ot starata shema (nyama kolona DirectorId), triem tablicata i ya sazdavame nanovo
        long cols, hasDirectorId;
        using (var check = connection.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*), COALESCE(SUM(name = 'DirectorId'), 0) FROM pragma_table_info('Movies')";
            using var r = check.ExecuteReader();
            r.Read();
            cols = r.GetInt64(0);
            hasDirectorId = r.GetInt64(1);
        }
        var oldSchema = cols > 0 && hasDirectorId == 0;

        if (oldSchema)
        {
            using var drop = connection.CreateCommand();
            drop.CommandText = "DROP TABLE Movies;";
            drop.ExecuteNonQuery();
        }
        using (var cmd = connection.CreateCommand())
        {
            // 3) Genres i Directors se sazdavat PURVI, zashtoto Movies sochi kum tyah
            // ON DELETE RESTRICT = bazata ne pozvolyava iztrivane na zhanr/rejisyor, dokato ima film s nego
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Genres (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );

                CREATE TABLE IF NOT EXISTS Directors (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Country TEXT NOT NULL DEFAULT ''
                );

                CREATE TABLE IF NOT EXISTS Movies (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Year INTEGER NOT NULL,
                    Rating REAL NOT NULL,
                    Description TEXT NOT NULL DEFAULT '',
                    Actors TEXT NOT NULL DEFAULT '',
                    DirectorId INTEGER NOT NULL,
                    GenreId INTEGER NOT NULL,
                    FOREIGN KEY (DirectorId) REFERENCES Directors(Id) ON DELETE RESTRICT,
                    FOREIGN KEY (GenreId)    REFERENCES Genres(Id)    ON DELETE RESTRICT
                );
                """;
            cmd.ExecuteNonQuery();
        }

        if (isNewFile || oldSchema)   // <-- tuk e promenenoto uslovie
        {
            Seed(connection);
        }
    }

    // 4) purvo zhanrove i rejisyori, posle filmite (te se nuzhdaят ot tyah)
    private static void Seed(SqliteConnection connection)
    {
        string[] genres = { "Sci-Fi", "Crime", "Thriller", "Animation", "Action", "Comedy", "Drama", "Horror" };

        (string Name, string Country)[] directors =
        {
            ("Denis Villeneuve", "Canada"),
            ("Christopher Nolan", "UK"),
            ("Francis Ford Coppola", "USA"),
            ("Bong Joon-ho", "South Korea"),
            ("Hayao Miyazaki", "Japan"),
            ("Quentin Tarantino", "USA")
        };

        // rejisyorut i zhanrut se podavat kato ime; Id-to se namira s poddavane (subquery) pri INSERT
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

        foreach (var g in genres)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO Genres (Name) VALUES ($name)";
            cmd.Parameters.AddWithValue("$name", g);
            cmd.ExecuteNonQuery();
        }

        foreach (var d in directors)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO Directors (Name, Country) VALUES ($name, $country)";
            cmd.Parameters.AddWithValue("$name", d.Name);
            cmd.Parameters.AddWithValue("$country", d.Country);
            cmd.ExecuteNonQuery();
        }

        foreach (var s in samples)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO Movies (Title, Year, Rating, Description, Actors, DirectorId, GenreId)
                VALUES ($title, $year, $rating, $description, $actors,
                        (SELECT Id FROM Directors WHERE Name = $director),
                        (SELECT Id FROM Genres WHERE Name = $genre))
                """;
            cmd.Parameters.AddWithValue("$title", s.Title);
            cmd.Parameters.AddWithValue("$year", s.Year);
            cmd.Parameters.AddWithValue("$rating", s.Rating);
            cmd.Parameters.AddWithValue("$description", s.Description);
            cmd.Parameters.AddWithValue("$actors", s.Actors);
            cmd.Parameters.AddWithValue("$director", s.Director);
            cmd.Parameters.AddWithValue("$genre", s.Genre);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    // 5) prevrushta edin red ot MovieSelect v obekt Movie (indeksite sa po reda na kolonite v MovieSelect)
    private static Movie Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Title = r.GetString(1),
        Year = r.GetInt32(2),
        Rating = r.GetDouble(3),
        Description = r.IsDBNull(4) ? null : r.GetString(4),
        Actors = r.IsDBNull(5) ? null : r.GetString(5),
        DirectorId = r.GetInt32(6),
        GenreId = r.GetInt32(7),
        DirectorName = r.GetString(8),
        GenreName = r.GetString(9)
    };

    // 6) slaga stoinostite na filma kato parametri (praznite neizbezhdani poleta stavat '')
    private static void Bind(SqliteCommand cmd, Movie m)
    {
        cmd.Parameters.AddWithValue("$title", m.Title.Trim());
        cmd.Parameters.AddWithValue("$year", m.Year);
        cmd.Parameters.AddWithValue("$rating", m.Rating);
        cmd.Parameters.AddWithValue("$description",
            string.IsNullOrWhiteSpace(m.Description) ? string.Empty : m.Description.Trim());
        cmd.Parameters.AddWithValue("$actors",
            string.IsNullOrWhiteSpace(m.Actors) ? string.Empty : m.Actors.Trim());
        cmd.Parameters.AddWithValue("$directorId", m.DirectorId);
        cmd.Parameters.AddWithValue("$genreId", m.GenreId);
    }

    // ---------------- Movies ----------------

    // Select
    public IReadOnlyList<Movie> GetAll()
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = MovieSelect + " ORDER BY m.Title COLLATE NOCASE";
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

        movie.DirectorId = GetOrCreateId(connection, "Directors", movie.DirectorName);
        movie.GenreId = GetOrCreateId(connection, "Genres", movie.GenreName);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = InsertSql + "; SELECT last_insert_rowid();";
        Bind(cmd, movie);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public bool Edit(Movie movie)
    {
        using var connection = Open();

        movie.DirectorId = GetOrCreateId(connection, "Directors", movie.DirectorName);
        movie.GenreId = GetOrCreateId(connection, "Genres", movie.GenreName);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
        UPDATE Movies
        SET Title = $title, Year = $year, Rating = $rating,
        Description = $description, Actors = $actors,
        DirectorId = $directorId, GenreId = $genreId
        WHERE Id = $id
        """;
        Bind(cmd, movie);
        cmd.Parameters.AddWithValue("$id", movie.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(int id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Movies WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    //vrushta detaili samo za edin film
    public Movie? GetById(int id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = MovieSelect + " WHERE m.Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    // Изпълнява динамична заявка от преводача за търсене с Gemini AI
    public IReadOnlyList<Movie> ExecuteSqlSelect(SqlSelectCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        using var connection = Open();
        using var cmd = connection.CreateCommand();

        cmd.CommandText = command.Sql;

        // Прехвърляме параметрите ($p0, $p1, $limit и т.н.)
        foreach (var (paramName, value) in command.Parameters)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = paramName;
            param.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }

        using var reader = cmd.ExecuteReader();
        var result = new List<Movie>();
        while (reader.Read())
        {
            // Използваме съществуващия ти помощен метод Map()
            result.Add(Map(reader));
        }

        return result;
    }

    // ima li drug film s sustoto zaglavie (bezId = pri redakciya ne se broi samiyat film)
    public bool TitleExists(string title, int bezId = 0)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM Movies WHERE Title = $title COLLATE NOCASE AND Id <> $id LIMIT 1";
        cmd.Parameters.AddWithValue("$title", title.Trim());
        cmd.Parameters.AddWithValue("$id", bezId);
        return cmd.ExecuteScalar() is not null;
    }

    // ima li drug rejisyor s sushtoto ime v tablica Directors
    public bool DirectorExists(string name, int bezId = 0)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM Directors WHERE Name = $name COLLATE NOCASE AND Id <> $id LIMIT 1";
        cmd.Parameters.AddWithValue("$name", name.Trim());
        cmd.Parameters.AddWithValue("$id", bezId);
        return cmd.ExecuteScalar() is not null;
    }

    // ---------------- Genres ----------------

    public IReadOnlyList<Genre> GetGenres()
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Genres ORDER BY Name COLLATE NOCASE";
        using var reader = cmd.ExecuteReader();
        var result = new List<Genre>();
        while (reader.Read())
        {
            result.Add(new Genre { Id = reader.GetInt32(0), Name = reader.GetString(1) });
        }
        return result;
    }

    public Genre? GetGenreById(int id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Genres WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? new Genre { Id = reader.GetInt32(0), Name = reader.GetString(1) } : null;
    }

    // hvurlq SqliteException, ako imeto veche sashtestvuva (UNIQUE) - kontrolerut go hvashta
    public int AddGenre(Genre genre)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Genres (Name) VALUES ($name); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$name", genre.Name.Trim());
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public bool UpdateGenre(Genre genre)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Genres SET Name = $name WHERE Id = $id";
        cmd.Parameters.AddWithValue("$name", genre.Name.Trim());
        cmd.Parameters.AddWithValue("$id", genre.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteGenre(int id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Genres WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }
  

 
    // ima li film s tozi zhanr (proverka predi iztrivane)
    public bool GenreInUse(int id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM Movies WHERE GenreId = $id LIMIT 1";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteScalar() is not null;
    }

    // ---------------- Directors ----------------

    private static Director MapDirector(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        Country = r.IsDBNull(2) ? null : r.GetString(2)
    };

    public IReadOnlyList<Director> GetDirectors()
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Country FROM Directors ORDER BY Name COLLATE NOCASE";
        using var reader = cmd.ExecuteReader();
        var result = new List<Director>();
        while (reader.Read())
        {
            result.Add(MapDirector(reader));
        }
        return result;
    }

    public Director? GetDirectorById(int id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Country FROM Directors WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapDirector(reader) : null;
    }

    public int AddDirector(Director director)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Directors (Name, Country) VALUES ($name, $country); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$name", director.Name.Trim());
        cmd.Parameters.AddWithValue("$country",
            string.IsNullOrWhiteSpace(director.Country) ? string.Empty : director.Country.Trim());
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public bool UpdateDirector(Director director)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Directors SET Name = $name, Country = $country WHERE Id = $id";
        cmd.Parameters.AddWithValue("$name", director.Name.Trim());
        cmd.Parameters.AddWithValue("$country",
            string.IsNullOrWhiteSpace(director.Country) ? string.Empty : director.Country.Trim());
        cmd.Parameters.AddWithValue("$id", director.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteDirector(int id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Directors WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DirectorInUse(int id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM Movies WHERE DirectorId = $id LIMIT 1";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteScalar() is not null; }

    // namira zapis po ime (bez znachenie ot registura za latinica); ako go nyama, go suzdava i vrushta Id-to
    // table idva samo ot vutreshni konstanti ("Genres"/"Directors"), imeto vinagi e parametur
    private static int GetOrCreateId(SqliteConnection connection, string table, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Unknown";
        }

        name = name.Trim();

        using (var find = connection.CreateCommand())
        {
            find.CommandText = $"SELECT Id FROM {table} WHERE Name = $name COLLATE NOCASE LIMIT 1";
            find.Parameters.AddWithValue("$name", name);
            if (find.ExecuteScalar() is long existingId)
            {
                return (int)existingId;
            }
        }

        using var insert = connection.CreateCommand();
        insert.CommandText = $"INSERT INTO {table} (Name) VALUES ($name); SELECT last_insert_rowid();";
        insert.Parameters.AddWithValue("$name", name);
        return Convert.ToInt32(insert.ExecuteScalar());
    }

    // podobni filmi: sushtiyat zhanr ili sushtiyat rejisyor.
    // Podrezhdane: purvo tezi, koito suvpadat i po dvete (2), posle po edno (1), posle po rejting.
    public IReadOnlyList<Movie> GetSimilar(int id, int limit = 4)
    {
        var movie = GetById(id);
        if (movie is null)
        {
            return new List<Movie>();
        }

        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = MovieSelect +
            " WHERE m.Id <> $id AND (m.GenreId = $genreId OR m.DirectorId = $directorId)" +
            " ORDER BY (m.GenreId = $genreId) + (m.DirectorId = $directorId) DESC, m.Rating DESC" +
            " LIMIT $limit";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$genreId", movie.GenreId);
        cmd.Parameters.AddWithValue("$directorId", movie.DirectorId);
        cmd.Parameters.AddWithValue("$limit", limit);

        using var reader = cmd.ExecuteReader();
        var result = new List<Movie>();
        while (reader.Read())
        {
            result.Add(Map(reader));
        }
        return result;
    }

}