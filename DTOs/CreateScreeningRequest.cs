namespace MoviesAPI.DTOs
{
    public class CreateScreeningRequest
    {
        public int MovieId { get; set; }
        public int HallId { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public List<TimeSpan> Times { get; set; }
    }
}
