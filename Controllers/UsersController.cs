using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoviesAPI.Data;
using MoviesAPI.DTOs;
using MoviesAPI.Models;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using static QRCoder.PayloadGenerator;

namespace MoviesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private static Dictionary<Guid,int> otpDict = new Dictionary<Guid,int>();
        private readonly AppDbContext _context;
        private readonly EmailSettings _settings;
        public UsersController(AppDbContext context, IOptions<EmailSettings> settings)
        {
            _context = context;
            _settings = settings.Value;
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest req)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(req.IdToken);

            var email = payload.Email;
            var name = payload.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Role=="user");

            if (user == null)
            {
                user = new Users
                {
                    Email = email,
                    Username = email,
                    Name = name,
                    Surname = payload.GivenName,
                    Role = "user",
                    Photo= payload.Picture,
                    Password= Guid.NewGuid().ToString(),
                    PasswordHash =""
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            return Ok(new LoginResponse
            {
                Status = "Success",
                Role = user.Role,
                Id = user.Id
            });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            List<Users> users = await _context.Users
                            .Where(u => u.Username == req.Username && u.Password == req.Password)
                            .ToListAsync();
            LoginResponse loginResponse = new LoginResponse();
            if (users.Count == 1)
            {
                loginResponse.Status = "Success";
                loginResponse.Role = users.FirstOrDefault().Role;
                loginResponse.Id = users.FirstOrDefault().Id;
                loginResponse.ChangePassword = (users.FirstOrDefault().Role == "admin" && users.FirstOrDefault().Password == _settings.AdminCode) ? true : false;
                return Ok(loginResponse);
            }
            loginResponse.Status = "User not found";
            loginResponse.Role = "";
            return BadRequest(loginResponse);

        }

        [HttpPost]
        [Route("signupotp")]
        public async Task<IActionResult> SignUpOTP([FromBody] OtpRequest request)
        {
            Guid key = Guid.NewGuid();
            int value = Random.Shared.Next(100000, 1000000);

            otpDict[key] = value;

           
            await SendOtpEmail(request.Email, value);

            return Ok(new { key });
        }

        private async Task SendOtpEmail(string email, int otp)
        {
            var message = new MailMessage();
            message.From = new MailAddress(
                _settings.From,
                _settings.DisplayName
            );

            message.To.Add(email);
            message.Subject = "Your Signup OTP";
            message.Body = $"Your OTP code is: {otp}";

            using var smtp = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort);
            smtp.Credentials = new NetworkCredential(
                _settings.Username,
                _settings.Password
            );
            smtp.EnableSsl = true;

            await smtp.SendMailAsync(message);
        }


        [HttpPost]
        [Route("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            if (!otpDict.TryGetValue(request.Key, out var storedOtp))
                return BadRequest("Invalid key");

            if (storedOtp != request.Otp)
                return BadRequest("Invalid OTP");

            otpDict.Remove(request.Key);

            if(request.Password == _settings.AdminCode)
            {
                Users checkUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Role=="admin");
                if (checkUser != null) 
                {
                    return BadRequest("User Already Exists");
                }
            }
            else
            {
                Users checkUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Role == "user");
                if (checkUser != null)
                {
                    return BadRequest("User Already Exists");
                }
            }

            Users user = new Users();
            user.Email = request.Email;
            user.Password = request.Password;
            user.Username = request.Email;
            user.Name = request.Name;
            user.Surname = request.Surname;
            user.Phone = request.Phone;
            user.PasswordHash = "";
            user.Role = request.Password==_settings.AdminCode? "admin":"user";
            user.Photo = "";

            _context.Users.Add(user);
            await _context.SaveChangesAsync();  

            return Ok("User created");
        }

        [HttpPut]
        [Route("changepass")]
        public async Task<IActionResult> ChangePass([FromBody] ChangePassRequest request)
        {
            if (!otpDict.TryGetValue(request.Key, out var storedOtp))
                return BadRequest("Invalid key");

            if (storedOtp != request.Otp)
                return BadRequest("Invalid OTP");

            otpDict.Remove(request.Key);

            Users user = _context.Users.FirstOrDefault(u => u.Username == request.email);
            if (user == null)
                 user =  _context.Users.Find(request.Id);

           

                if (user == null)
                return BadRequest();

            user.Password= request.Password;

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(request.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Ok("Password Change");
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Users>> GetUser(int id)
        {
            var User = await _context.Users.FindAsync(id);

            if (User == null)
            {
                return NotFound();
            }

            return User;
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadPhoto([FromForm] UploadPhotoDto dto)
        {
            if (dto.Photo == null || dto.Photo.Length == 0)
                return BadRequest("No file uploaded");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.Id);

            if (user == null)
                return NotFound("User not found");

            
            var uploadsPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");

            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            
            var extension = Path.GetExtension(dto.Photo.FileName);
            var fileName = $"user_{dto.Id}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Photo.CopyToAsync(stream);
            }


            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var imageUrl = $"{baseUrl}/uploads/{fileName}";

            
            user.Photo = imageUrl;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                userId = user.Id,
                photoUrl = imageUrl
            });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, Users user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpGet()]
        public async Task<ActionResult<IEnumerable<Users>>> GetUsers()
        {
            List<Users> users = await _context.Users.ToListAsync();
            return Ok(users);

        }

    }
}
