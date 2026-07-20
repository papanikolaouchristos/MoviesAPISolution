using MoviesAPI.Models;

namespace MoviesAPI.DTOs
{
    public class ScreeningDto
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public Movies Movie {  get; set; }
        public int HallId { get; set; }
        public Halls Hall { get; set; } 
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public List<ShowTimeDto> ShowTimes { get; set; }
    }

    public class ShowTimeDto
    {
        public int Id { get; set; }
        public TimeSpan Time { get; set; }
    }
}
