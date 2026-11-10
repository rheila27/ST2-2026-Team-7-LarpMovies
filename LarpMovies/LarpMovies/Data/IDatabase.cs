using LarpMovies.Models;
using LarpMovies.Services;

namespace LarpMovies.Data;


/// Obedinyava vsichki hranilishta na edno. Realizira se ot Database (Singleton),
/// kakto se registrira v Program.cs. Kontrolerite polzvat samo tochnoto hranilishte, ot koeto se nuzhdaят.
public interface IDatabase : IMovieRepository, IGenreRepository, IDirectorRepository 
{
    IReadOnlyList<Movie> ExecuteSqlSelect(SqlSelectCommand command);
}

