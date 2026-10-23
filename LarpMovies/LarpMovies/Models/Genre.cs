using System.ComponentModel.DataAnnotations; //za da imame required i range nagotovo

namespace LarpMovies.Models
{
    public class Genre
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name of the genre is required.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Director must be 2 to 80 characters.")]
        public string Name { get; set; } = "";
    }
}
