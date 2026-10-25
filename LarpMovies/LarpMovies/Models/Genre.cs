using System.ComponentModel.DataAnnotations; //za da imame required i range nagotovo

namespace LarpMovies.Models
{
    public class Genre
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name of the genre is required.")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "Genre must be 2 to 40 characters.")]
        public string Name { get; set; } = "";
    }
}
