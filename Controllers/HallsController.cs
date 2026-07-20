using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoviesAPI.Data;
using MoviesAPI.DTOs;
using MoviesAPI.Models;
using QRCoder;
using System.Net;
using System.Net.Mail;

namespace MoviesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HallsController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly EmailSettings _settings;
        public HallsController(AppDbContext context, IOptions<EmailSettings> settings)
        {
            _context = context;
            _settings = settings.Value;
        }

        [HttpPost("hall")]
        public async Task<ActionResult<Halls>> CreateHall(CreateHallRequest req)
        {
            Halls hall = new Halls
            {
                Name = req.Name,
                Rows = req.Rows,
                Columns = req.Columns,
                Seats = new List<Seats>()
            };

            for (int r = 1; r <= req.Rows; r++)
            {
                for (int c = 1; c <= req.Columns; c++)
                {
                    hall.Seats.Add(new Seats
                    {
                        Row = r,
                        Column = c
                    });
                }
            }

            _context.Halls.Add(hall);
            await _context.SaveChangesAsync();
            hall.Seats = null;

            return Ok(hall);
        }

        [HttpGet("hall")]
        public async Task<IActionResult> GetHalls()
        {
            var halls = await _context.Halls.ToListAsync();
            return Ok(halls);
        }

        [HttpGet("booking")]
        public async Task<IActionResult> GetBooking()
        {
            var booking = await _context.Booking
                .Include(b=>b.Seat)
                .Include(h=>h.Hall)
                .Include(s=>s.Screening)
                .ThenInclude(m=>m.Movie)
                .ToListAsync();
            foreach(var b in booking)
            {
                b.Seat.Hall = null;
            }
            return Ok(booking);
        }
        [HttpGet("booking/{id}")]
        public async Task<IActionResult> GetBookingByUserId(int id)
        {
            var booking = await _context.Booking
                .Include(s=>s.Seat)
                .Include(h => h.Hall)
                .Include(s => s.Screening)
                .ThenInclude(m => m.Movie)
                .Where(m => m.UserId == id) 
                .ToListAsync();

            foreach (var b in booking)
            {
               
                b.Seat.Hall = null;
                b.Hall.Seats = null;
                b.Screening.Hall.Seats = null;

            }

            return Ok(booking);
        }

        [HttpDelete("booking/{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Booking.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            _context.Booking.Remove(booking);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("screening")]
        public async Task<IActionResult> CreateScreening(CreateScreeningRequest req)
        {
            Screenings screening = new Screenings
            {
                MovieId = req.MovieId,
                HallId = req.HallId,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                ShowTimes = new List<ShowTimes>()
            };

            foreach (var time in req.Times)
            {
                screening.ShowTimes.Add(new ShowTimes
                {
                    Time = time
                });
            }

            _context.Screenings.Add(screening);
            await _context.SaveChangesAsync();

            return Ok("Screening created");
        }

        [HttpGet("screening")]
        public async Task<IActionResult> GetScreenings()
        {
            var screenings = await _context.Screenings
                .Include(m => m.Movie)
                  .ThenInclude(m => m.Category)
                .Include(s => s.ShowTimes)
                .Include(h => h.Hall)
                .ToListAsync();

            var result = screenings.Select(s => new ScreeningDto
            {
                Id = s.Id,
                MovieId = s.MovieId,
                Movie = s.Movie,
                HallId = s.HallId,
                Hall = s.Hall,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                ShowTimes = s.ShowTimes.Select(st => new ShowTimeDto
                {
                    Id = st.Id,
                    Time = st.Time
                }).ToList()
            });

            result = result.Where(s => s.StartDate <= DateOnly.FromDateTime(DateTime.Now) && s.EndDate >= DateOnly.FromDateTime(DateTime.Now)).ToList();
            return Ok(result);
        }


        [HttpGet("seats/{id}/{date}/{time}")]
        public async Task<ActionResult<List<Seats>>> GetSeatByMovieId(int id, DateOnly date, TimeSpan time)
        {
            var seats = await _context.Seats
                             .Where(s => s.HallId == id)
                             .ToListAsync();

            List<Booking> booking = await _context.Booking
                                      .Where(b => b.HallId == id && b.Date == date && b.Time == time)
                                      .ToListAsync();

            foreach (Seats seat in seats)
            {
                if (booking.Exists(b => b.SeatId == seat.Id))
                {
                    seat.IsBooked = true;

                }

            }

            if (seats.Count == 0)
            {
                return NotFound();
            }

            return seats;
        }

        [HttpPost("booking")]
        public async Task<IActionResult> BookingSeat(BookingRequest req)
        {
            Booking booking = new Booking
            {
                SeatId = req.SeatId,
                ScreeningId = req.ScreeningId,
                UserId = req.UserId,
                HallId = req.HallId,
                Date = req.Date,
                Time = req.Time,
                ChekedIn = false
            };

            _context.Booking.Add(booking);
            await _context.SaveChangesAsync();
            return Ok("Booling Seat OK!");
        }


        [HttpGet("extramovieinfo")]
        public async Task<IActionResult> GetExtraMovieInfo()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var data = await _context.Screenings
                .Include(s => s.Hall)
                .Where(s =>
                    s.StartDate <= today &&
                    s.EndDate >= today
                )
                .Select(s => new
                {
                    s.MovieId,
                    HallName = s.Hall.Name
                })
                .ToListAsync(); 

            var result = data
                .GroupBy(x => x.MovieId)
                .Select(g => new ExtraMovieInfo
                {
                    MovieId = g.Key,
                    Halls = string.Join(" | ",
                        g.Select(x => x.HallName).Distinct()
                    )
                })
                .ToList();

            return Ok(result);
        }

        [HttpPut("checkin/{id}")]
        public async Task<IActionResult> CheckIn(int id)
        {
            Booking? booking = await _context.Booking.FindAsync(id);
            booking.ChekedIn = true;
            _context.Entry(booking).State = EntityState.Modified;


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookingExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Ok();
        }


        [HttpGet("checkin-qr/{id}")]
        public async Task<IActionResult> CheckInFromQr(int id)
        {
            Booking? booking = await _context.Booking.FindAsync(id);
            if (booking == null)
                return NotFound("Δεν βρέθηκε κράτηση.");

            booking.ChekedIn = true;
            await _context.SaveChangesAsync();

            return Content("✅ Επιτυχές check-in! Μπορείς να κλείσεις αυτή τη σελίδα.");
        }

        [HttpGet("send-checkin-qr/{id}")]
        public async Task<IActionResult> SendCheckInQr(int id)
        {
            Booking? booking = await _context.Booking.Include(b => b.User).Where(b=>b.Id==id).FirstOrDefaultAsync();
            if (booking == null)
                return NotFound("Δεν βρέθηκε κράτηση.");

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkinUrl = $"{baseUrl}/api/Halls/checkin-qr/{id}";

            // Δημιουργία QR ως byte[]
            byte[] qrBytes = GenerateQrCode(checkinUrl);

            var subject = "Το QR για το check-in σου 🎟️";
            var body = $@"
                 Γεια σου {booking.User.Name} {booking.User.Surname},

                   Σκάναρε το QR που επισυνάπτεται για να κάνεις check-in.

                   Εναλλακτικά, πάτησε εδώ:
                     {checkinUrl}

                    Καλή διασκέδαση!
                                  ";

            await SendQREmail(booking.User.Email, subject, body, qrBytes);

            return Ok();
        }


        private bool BookingExists(int id)
        {
            return _context.Booking.Any(e => e.Id == id);
        }

        private async Task SendQREmail(string email,string subject,string body, byte[] qrBytes)
        {
            var message = new MailMessage();
            message.From = new MailAddress(
                 _settings.From,
                _settings.DisplayName
            );

            message.To.Add(email);
            message.Subject = subject;
            var htmlBody = $@"
                   <h2>Το QR για το check-in σου</h2>
                         <p>{body}</p>
                      <img src='cid:qrImage' width='200' height='200' style='max-width:200px; height:auto;' />
                                              ";

            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");

            var qrStream = new MemoryStream(qrBytes);
            var linkedResource = new LinkedResource(qrStream, "image/png")
            {
                ContentId = "qrImage"
            };

            htmlView.LinkedResources.Add(linkedResource);
            message.AlternateViews.Add(htmlView);
            message.IsBodyHtml = true;


            using var smtp = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort);
            smtp.Credentials = new NetworkCredential(
                 _settings.Username,
                 _settings.Password
            );
            smtp.EnableSsl = true;

            await smtp.SendMailAsync(message);
        }

        private byte[] GenerateQrCode(string url)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            return qrCode.GetGraphic(6);
        }

    }
}
