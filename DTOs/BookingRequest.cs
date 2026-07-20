namespace MoviesAPI.DTOs
{
    public class BookingRequest
    {
        public int SeatId { get; set; }
        public int ScreeningId { get; set; }
        public int HallId { get; set; } 
        public DateOnly Date {  get; set; }
        public TimeSpan Time { get; set; }
        public int UserId { get; set; } 
    }
}
