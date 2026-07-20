namespace MoviesAPI.DTOs
{
    public class EmailSettings
    {
        public string From { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string SmtpHost { get; set; } = null!;
        public int SmtpPort { get; set; }
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string AdminCode { get; set; } = null!;
    }
}
