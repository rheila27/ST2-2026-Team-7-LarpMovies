using LarpMovies.Data;
using LarpMovies.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LarpMovies.Controllers;

public class MoviesController : Controller
{
    private readonly IDatabase _db;

    public MoviesController(IDatabase db) => _db = db;

    // GET /Movies -> stranicata sas spisuka (tablicata se pulni ot JavaScript)
    public IActionResult Index() => View();

    // GET /Movies/List -> vsichki filmi kato JSON za movies.js (filturut e v brauzura)
    [HttpGet]
    public IActionResult List() => Json(_db.GetAll());

    // GET /Movies/Create -> prazna forma
    

    // POST /Movies/Create -> proverka i zapis
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(Movie movie)
    {
        if(!ModelState.IsValid)
        {
            LoadLists(movie.DirectorId, movie.GenreId);
            return View(movie);
        }

        _db.Add(movie);
        TempData["Message"] = $"Movie '{movie.Title}' was added.";
        return RedirectToAction(nameof(Index));
    }

    //iztrivane za UI, potrebitelqt potvurjdava
    [HttpGet]
    public IActionResult Delete(int id)
    {
        var movie = _db.GetById(id);
        return movie is null ? NotFound() : View(movie);
    }

    // iztrivane ot bazata
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public IActionResult DeleteMovie(int id)
    {
        if (!_db.Delete(id))
        {
            return NotFound();
        }

        TempData["Message"] = "Movie deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // pulni padashtite menyuta s rejisyori i zhanrove ot bazata
    private void LoadLists(int? selectedDirector = null, int? selectedGenre = null)
    {
        ViewBag.Directors = new SelectList(_db.GetDirectors(), "Id", "Name", selectedDirector);
        ViewBag.Genres = new SelectList(_db.GetGenres(), "Id", "Name", selectedGenre);
    }
 
    // Edit GET: избира текущите стойности на филма
    public IActionResult Edit(int id)
    {
        var movie = _db.GetById(id);
        if (movie is null) return NotFound();
        LoadLists(movie.DirectorId, movie.GenreId);
        return View(movie);
    }

    /*public IActionResult Edit(int id)
    {
        var movie = _db.GetAll().FirstOrDefault(m => m.Id == id);
        if (movie is null)
        {
            return NotFound();
        }
        return View(movie);
    } */

}

