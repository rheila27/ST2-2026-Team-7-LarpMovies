using LarpMovies.Models;

namespace LarpMovies.Data;


/// Design pattern: REPOSITORY.
/// Problem: kontrolerite ne trqbva da znaqt kak i kude se pazyat filmite (SQLite, SQL, tablici).
/// Reshenie: edin dogovor za dostup do filmite; kontrolerut vizhda samo tozi interfeis.
/// Realizaciya: Database (SQLite). Za da smenim bazata, promenyame samo realizaciyata.
public interface IMovieRepository
{
    IReadOnlyList<Movie> GetAll();
    //chete redovete ot Movies i gi slaga v spisuka. IReadOnlyList kazva, che tozi spisuk moje samo da se chete
    Movie? GetById(int id);
    int Add(Movie movie);
    bool Edit(Movie movie);
    bool Delete(int id);

    // za da ne se povtarqt filmi
    bool TitleExists(string title, int bezId = 0);
}