using LarpMovies.Models;
using LarpMovies.Services;

namespace LarpMovies.Data
{
    ///Repository za rejisyorite (IMovieRepository za opisanie na shablona)
    public interface IDirectorRepository
    {
        IReadOnlyList<Director> GetDirectors();
        Director? GetDirectorById(int id);
        int AddDirector(Director director);
        bool UpdateDirector(Director director);
        bool DeleteDirector(int id);
        bool DirectorInUse(int id);

        bool DirectorExists(string name, int bezId = 0);
    }
}
