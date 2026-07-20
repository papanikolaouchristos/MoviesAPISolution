namespace MoviesAPI.Models
{
    public class Categories
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;

       // public ICollection<Movies> MoviesList { get; set; } = new List<Movies>();
    }
}
