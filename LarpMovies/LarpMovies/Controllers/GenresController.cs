using LarpMovies.Data;
using LarpMovies.Models;
using Microsoft.AspNetCore.Mvc;

namespace LarpMovies.Controllers;

public class GenresController : Controller
{
    private readonly IDatabase _db;

    public GenresController(IDatabase db) => _db = db;

    // GET /Genres -> stranicata sas spisuka (tablicata se pulni ot JavaScript)
    public IActionResult Index() => View();

    // GET /Genres/List -> vsichki janri kato JSON za genres.js (filturut e v brauzura)
    [HttpGet]
    public IActionResult List() => Json(_db.GetGenres());

    // GET /Genres/Create -> prazna forma
    public IActionResult Create() => View(new Genre());

    // POST /Genres/Create -> proverka i zapis
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(Genre genre)
    {
        if (!ModelState.IsValid)
        {
            return View(genre);
        }

        _db.AddGenre(genre);
        TempData["Message"] = $"Genre '{genre.Name}' was added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Genre genre)
    {
        if (id != genre.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid) //proverkata se izvurshva pri uniquemovie klasa
        {
            return View(genre);
        }

        _db.UpdateGenre(genre);
        TempData["Message"] = $"Genre '{genre.Name}' was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var movie = _db.GetAll().FirstOrDefault(m => m.Id == id);
        if (movie is null)
        {
            return NotFound();
        }
        return View(movie);
    }

    //iztrivane za UI, potrebitelqt potvurjdava
    [HttpGet]
    public IActionResult Delete(int id)
    {
        var genre = _db.GetGenreById(id);
        return genre is null ? NotFound() : View(genre);
    }

    // iztrivane ot bazata
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public IActionResult DeleteGenre(int id)
    {
        if (!_db.DeleteGenre(id))
        {
            return NotFound();
        }

        TempData["Message"] = "Genre deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}

