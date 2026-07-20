namespace MoviesAPI.DTOs
{
    public class SignUpRequest
    {
        public Guid Key { get; set; }
        public int Otp { get; set; }

        public string Name { get; set; }
        public string Surname { get; set; }
        public long Phone {  get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
