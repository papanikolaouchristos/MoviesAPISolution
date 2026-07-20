namespace MoviesAPI.DTOs
{
    public class ChangePassRequest
    {
        public Guid Key { get; set; }
        public int Otp { get; set; }
        public int Id { get; set; }
        public string Password { get; set; }
        public string email { get; set; }

    }
}
