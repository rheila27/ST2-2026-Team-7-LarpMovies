using LarpMovies.Models;

namespace LarpMovies.Data
{
    
        /// Repository za zhanrovete (vijte IMovieRepository za opisanie na shablona)
        public interface IGenreRepository
        {
            IReadOnlyList<Genre> GetGenres();
            Genre? GetGenreById(int id);
            int AddGenre(Genre genre);
            bool UpdateGenre(Genre genre);
            bool DeleteGenre(int id);
            bool GenreInUse(int id);
        }
    
}
