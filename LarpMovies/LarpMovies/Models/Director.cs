using System.ComponentModel.DataAnnotations;

namespace LarpMovies.Models;

public class Director
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Director name is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be 2 to 80 characters.")]
    public string Name { get; set; } = "";

    [StringLength(60, ErrorMessage = "Country can be up to 60 characters.")]
    public string? Country { get; set; }   // moje da ne se vuvejda zatova e sys string? (nullable)
}