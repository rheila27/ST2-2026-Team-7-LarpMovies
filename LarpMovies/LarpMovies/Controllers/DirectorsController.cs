using LarpMovies.Data;
using LarpMovies.Models;
using Microsoft.AspNetCore.Mvc;

namespace LarpMovies.Controllers;

public class DirectorsController : Controller
{
    private readonly IDatabase _db;

    public DirectorsController(IDatabase db) => _db = db;

    // GET /Directors -> stranicata sas spisuka (tablicata se pulni ot JavaScript)
    public IActionResult Index() => View();

    // GET /Directors/List -> vsichki rezhissori kato JSON za directors.js (filturut e v brauzura)
    [HttpGet]
    public IActionResult List() => Json(_db.GetDirectors());

    // GET /Directors/Create -> prazna forma
    public IActionResult Create() => View(new Director());

    // POST /Directors/Create -> proverka i zapis
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(Director director)
    {
        if (!ModelState.IsValid)
        {
            return View(director);
        }

        _db.AddDirector(director);
        TempData["Message"] = $"Director '{director.Name}' was added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Director director)
    {
        if (id != director.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid) //proverkata se izvurshva pri uniquemovie klasa
        {
            return View(director);
        }

        _db.UpdateDirector(director);
        TempData["Message"] = $"Director '{director.Name}' was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var director = _db.GetDirectorById(id);
        return director is null ? NotFound() : View(director);
    }

    //iztrivane za UI, potrebitelqt potvurjdava
    [HttpGet]
    public IActionResult Delete(int id)
    {
        var director = _db.GetDirectorById(id);
        return director is null ? NotFound() : View(director);
    }

    // iztrivane ot bazata
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public IActionResult DeleteDirector(int id)
    {
        if (!_db.DeleteDirector(id))
        {
            return NotFound();
        }

        TempData["Message"] = "Director deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}

