namespace MoviesAPI.DTOs
{
    public class UploadPhotoDto
    {
        public int Id { get; set; }
        public IFormFile Photo { get; set; }
    }
}
