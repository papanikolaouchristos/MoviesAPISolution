using Microsoft.EntityFrameworkCore;
using MoviesAPI.Models;

namespace MoviesAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options): base(options) { }

        public DbSet<Movies> Movies { get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Halls> Halls { get; set; }
        public DbSet<Seats> Seats { get; set; }
        public DbSet<Screenings> Screenings { get; set; }
        public DbSet<ShowTimes> ShowTimes { get; set; }
        public DbSet<Booking> Booking { get; set; }


    }
}
