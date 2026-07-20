namespace MoviesAPI.DTOs
{
    public class CreateHallRequest
    {
        public string Name { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
    }
}
