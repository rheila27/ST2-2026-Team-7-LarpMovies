using LarpMovies.Data;
using LarpMovies.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace LarpMovies.Controllers;

public class MoviesController : Controller
{
    private readonly IDatabase _db;

    public MoviesController(IDatabase db) => _db = db;

    public IActionResult Index() => View();

    // JSON za JavaScript tablicata (filturut se pravi v brauzura).
    [HttpGet]
    public IActionResult List() => Json(_db.GetAll());

    public IActionResult Create() => View(new Movie { Year = DateTime.Now.Year });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(Movie movie)
    {
        if (!ModelState.IsValid)
        {
            return View(movie);
        }

        _db.Add(movie);
        TempData["Message"] = $"Movie '{movie.Title}' was added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Movie movie)
    {
        if (id != movie.Id)
        {
            return NotFound();
        }

        if (!(ModelState.IsValid || SaveMovie(() => _db.Edit(movie), movie)))
        {
            return View(movie);
        }

        _db.Edit(movie);
        TempData["Message"] = $"Movie '{movie.Title}' was updated.";
        return RedirectToAction(nameof(Index));
    }

    //za prepravqne kogato ne e 3 chasa!!!!!!!!
    //proverka pri edit da ne se update-vat ednakvi filmi ili pri create da nqma povtaracki
    private bool SaveMovie(Action save, Movie movie)
    {
        try
        {
            save();
            return true;
        }
        catch (SqliteException ex) 
        {
            ModelState.AddModelError(string.Empty, "it dont work like that sweetie");
            return false;
        }
    }
}