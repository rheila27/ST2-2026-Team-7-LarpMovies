using System.ComponentModel.DataAnnotations;

namespace LarpMovies.Models;

public class Genre
{
    public int Id { get; set; }  

    [Required(ErrorMessage = "Genre name is required.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Genre must be 2 to 60 characters.")]
    public string Name { get; set; } = "";
}