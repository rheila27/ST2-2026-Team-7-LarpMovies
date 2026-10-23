using LarpMovies.Data;
using LarpMovies.Models;
using Microsoft.AspNetCore.Mvc;

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
}