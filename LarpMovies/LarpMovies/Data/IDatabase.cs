using LarpMovies.Models;

namespace LarpMovies.Data;

public interface IDatabase
{
    IReadOnlyList<Movie> GetAll();
    int Add(Movie movie);

    bool Edit(Movie movie);

    //za tova da ne se povtarqt filmi
    bool TitleExists(string title, int bezId = 0);
    bool DirectorExists(string director, int bezId = 0);
}
//chete redovete ot Movies i gi slaga v spisuka. IReadOnlyList kazva, che tozi spisuk moje samo da se chete, i ako napishesh list.Add ili list.Remove shte dade greshka
