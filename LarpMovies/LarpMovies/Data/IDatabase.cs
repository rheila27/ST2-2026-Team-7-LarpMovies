using LarpMovies.Models;

namespace LarpMovies.Data;

public interface IDatabase
{
    IReadOnlyList<Movie> GetAll();
    Movie? GetById(int id);
    int Add(Movie movie);
    bool Edit(Movie movie);
    bool Delete(int id);

    // za da ne se povtarqt filmi
    bool TitleExists(string title, int bezId = 0);
    bool DirectorExists(string director, int bezId = 0);
}