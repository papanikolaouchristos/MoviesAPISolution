namespace MoviesAPI.Models
{
    public class Screenings
    {
        public int Id { get; set; }

        public int MovieId { get; set; }
        public Movies Movie { get; set; }

        public int HallId { get; set; }
        public Halls Hall { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public ICollection<ShowTimes> ShowTimes { get; set; }
    }
}
