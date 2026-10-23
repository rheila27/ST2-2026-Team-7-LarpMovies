using System.ComponentModel.DataAnnotations; //za da imame required i range nagotovo

namespace LarpMovies.Models;

public class Movie
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be 2 to 100 characters.")]
    public string Title { get; set; } = ""; //za da go chete prazen tekst a ne nujll, ne moje null gorl

    [Required(ErrorMessage = "Director is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Director must be 2 to 80 characters.")]
    public string Director { get; set; } = "";

    [Range(1888, 2027, ErrorMessage = "Year must be between 1888 and 2027.")]
    public int Year { get; set; }

    [Required(ErrorMessage = "Genre is required.")]
    [StringLength(40, ErrorMessage = "Genre can be up to 40 characters.")]
    public string Genre { get; set; } = "";

    [Range(0.0, 10.0, ErrorMessage = "Rating must be between 0 and 10.")]
    public double Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Description can be up to 1000 characters.")]
    public string? Description { get; set; } //moje da bude ostaveno prazno bez da chupi nishto hopefully

    [StringLength(300, ErrorMessage = "Actors can be up to 300 characters.")]
    public string? Actors { get; set; }

}
