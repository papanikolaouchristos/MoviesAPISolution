using Microsoft.EntityFrameworkCore;

namespace MoviesAPI.Models
{
    public class Movies
    {        
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Director { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int Year { get; set; }
        public int Age { get; set; }
        public string Image {  get; set; }  

        public int CategoryId { get; set; }
        public Categories? Category { get; set; }
    }
}
