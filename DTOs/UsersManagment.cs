namespace MoviesAPI.DTOs
{
    public class LoginResponse
    {
        public string Status {  get; set; }
        public string Role { get; set; }
        public int Id { get; set; }
        public bool ChangePassword { get; set; } = false;

    }
}
