namespace MoviesAPI.Models
{
    public class Halls
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int Rows { get; set; }
        public int Columns { get; set; }

        public ICollection<Seats> Seats { get; set; }
    }
}
