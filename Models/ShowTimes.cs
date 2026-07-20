namespace MoviesAPI.Models
{
    public class ShowTimes
    {
        public int Id { get; set; }

        public int ScreeningId { get; set; }
        public Screenings Screening { get; set; }

        public TimeSpan Time { get; set; }
    }
}
