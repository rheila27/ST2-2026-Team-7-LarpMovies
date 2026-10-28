using LarpMovies.Data;
using LarpMovies.Models;
using Microsoft.AspNetCore.Mvc;

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
    public IActionResult Create() => View(new Movie { Year = DateTime.Now.Year });

    // POST /Movies/Create -> proverka i zapis
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

    // GET /Movies/Delete/5 -> stranica za potvurzhdenie (ne trie nishto)
    public IActionResult Delete(int id) =>
        _db.GetById(id) is { } movie ? View(movie) : NotFound();

    // POST /Movies/Delete/5 -> realnoto iztrivane
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        if (_db.Delete(id))
        {
            TempData["Message"] = "Movie was deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}