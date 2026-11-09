using LarpMovies.Models;

namespace LarpMovies.Data;

public interface IDatabase
{
    IReadOnlyList<Movie> GetAll();
    int Add(Movie movie);
    //chete redovete ot Movies i gi slaga v spisuka. IReadOnlyList kazva, che tozi spisuk moje samo da se chete, i ako napishesh list.Add ili list.Remove shte dade greshka

    IReadOnlyList<Genre> GetGenres();
    Genre? GetGenreById(int id);
    int AddGenre(Genre genre);
    bool UpdateGenre(Genre genre);
    bool DeleteGenre(int id);
    bool GenreInUse(int id);

    IReadOnlyList<Director> GetDirectors();
    Director? GetDirectorById(int id);
    int AddDirector(Director director);
    bool UpdateDirector(Director director);
    bool DeleteDirector(int id);
    bool DirectorInUse(int id);
}

