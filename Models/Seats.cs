using System;

namespace MoviesAPI.Models
{
    public class Seats
    {
        public int Id { get; set; }

        public int Row { get; set; }      
        public int Column { get; set; }   
        public bool IsBooked { get; set; }  

        public int HallId { get; set; }
        public Halls Hall { get; set; }
    }
}
