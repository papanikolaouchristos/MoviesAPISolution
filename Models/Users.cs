namespace MoviesAPI.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public long  Phone {  get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; }
        public string Photo { get; set; }=string.Empty;
        public string Role { get; set; }

    }
}
