using LarpMovies.Data;
using LarpMovies.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace LarpMovies.Controllers;

public class MoviesController : Controller
{
    // Repository: kontrolerut poznava samo hranilishteto za filmi, ne i celiya Database
    private readonly IMovieRepository _db;

    public MoviesController(IMovieRepository db) => _db = db;

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

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Movie movie)
    {
        if (id != movie.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid) //proverkata se izvurshva pri uniquemovie klasa
        {
            return View(movie);
        }

        _db.Edit(movie);
        TempData["Message"] = $"Movie '{movie.Title}' was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var movie = _db.GetById(id);
        return movie is null ? NotFound() : View(movie);
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

    // GET /Movies/Details/5 -> pokazva detalite za filmite :)
    [HttpGet]
    public IActionResult Details(int id)
    {
        var movie = _db.GetById(id);
        return movie is null ? NotFound() : View(movie);
    }

    //metod za izvikvane  na random film ot bazata, za da se pokazva na nachalnata stranica

    // GET /Movies/SurpriseMe -> izbira sluchayen film i prenasochva kym Details
    [HttpGet]
    public IActionResult SurpriseMe()
    {
        // 1. Vzimame vsichki filmi ot bazata prez _db repository-to
        var movies = _db.GetAll()?.ToList();

        // 2. Proveriavame dali imame filmi v bazata
        if (movies == null || !movies.Any())
        {
            TempData["Message"] = "Nqma namereni filmi za izbor.";
            return RedirectToAction(nameof(Index));
        }

        // 3. Izbirame sluchayen indeks
        var random = new Random();
        int randomIndex = random.Next(movies.Count);
        var randomMovie = movies[randomIndex];

        // 4. Prenasochvame kym Details stranicata na sluchajnia film
        return RedirectToAction(nameof(Details), new { id = randomMovie.Id });
    }

}