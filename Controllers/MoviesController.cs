using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesAPI.Data;
using MoviesAPI.Models;



namespace MoviesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public MoviesController(AppDbContext context)
        {
            _context = context;
        }


        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Movies>>> GetMovies()
        {
            return await _context.Movies
           .Include(m => m.Category) 
           .ToListAsync();
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<Movies>> GetMovie(int id)
        {
            var movie = await _context.Movies
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            return movie;
        }

        
        
        [HttpPost]
        public async Task<ActionResult<Movies>> PostMovie(Movies movie)
        {

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMovie", new { id = movie.Id }, movie);
        }

        
        
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMovie(int id, Movies movie)
        {
            if (id != movie.Id)
            {
                return BadRequest();
            }

            _context.Entry(movie).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MovieExists(id))
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

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }

            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MovieExists(int id)
        {
            return _context.Movies.Any(e => e.Id == id);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string title)
        {
            string query =
                $"SELECT * FROM Movies WHERE Title LIKE '%{title}%'";
             //
            List<Movies> movies = await _context.Movies
                .FromSqlRaw(query)
                .ToListAsync();

            return Ok(movies);
        }
    }
}
