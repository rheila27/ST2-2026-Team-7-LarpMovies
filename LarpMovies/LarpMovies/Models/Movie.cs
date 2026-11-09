using System.ComponentModel.DataAnnotations;
using LarpMovies.Data; 

namespace LarpMovies.Models;

public class Movie
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be 2 to 100 characters.")]
    [UniqueMovieTitle]
    public string Title { get; set; } = ""; //za da go chete prazen tekst a ne nujll, ne moje null gorl

    // vutreshni klyuchove za Movies.
    // Range(1, ...) ne pozvolqva 0 za padashtiq spisuk posle

    
    public int DirectorId { get; set; }

    [Range(1888, 2027, ErrorMessage = "Year must be between 1888 and 2027.")]
    public int Year { get; set; }

    
    public int GenreId { get; set; }

    [Range(0.0, 10.0, ErrorMessage = "Rating must be between 0 and 10.")]
    public double Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Description can be up to 1000 characters.")]
    public string? Description { get; set; } //moje da bude ostaveno prazno bez da chupi nishto hopefully

    [StringLength(300, ErrorMessage = "Actors can be up to 300 characters.")]
    public string? Actors { get; set; }

    // Само за показване(идват от JOIN). Задължително са string?, иначе ASP.NET Core
    // ги счита за Required (nullable reference types) и формата никога не минава валидация.
    [Required(ErrorMessage = "Director name is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Director name must be between 2 and 80 characters.")]
    public string DirectorName { get; set; } = "";

    [Required(ErrorMessage = "Genre is required.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Genre must be between 2 and 60 characters.")]
    public string GenreName { get; set; } = "";

}
