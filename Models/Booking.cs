namespace MoviesAPI.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int SeatId { get; set; }
        public Seats Seat { get; set; }
        public int ScreeningId { get; set; }
        public Screenings Screening { get; set; }
        public int HallId { get; set; }
        public Halls Hall { get; set; }    
        public DateOnly Date { get; set; }
        public TimeSpan Time { get; set; }
        public int UserId { get; set; }
        public Users User { get; set; }
        public bool ChekedIn {  get; set; }
    }
}
