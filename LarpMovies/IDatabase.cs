using LarpMovies.Models;

namespace LarpMovies.Data;

public interface IDatabase
{
    IReadOnlyList<Movie> GetAll();
    int Add(Movie movie);
}
//chete redovete ot Movies i gi slaga v spisuka. IReadOnlyList kazva, che tozi spisuk moje samo da se chete, i ako napishesh list.Add ili list.Remove shte dade greshka