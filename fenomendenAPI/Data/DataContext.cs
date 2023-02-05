using Microsoft.EntityFrameworkCore;
using fenomendenAPI.Model;

namespace fenomendenAPI.Data
{
    public class DataContext:DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options){}

        public DbSet<Vehicle> vehicles { get; set; }
        public DbSet<User> users { get; set; }
    }
}