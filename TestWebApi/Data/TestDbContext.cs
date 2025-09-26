using Microsoft.EntityFrameworkCore;
using TestWebApi.Models;

namespace TestWebApi.Data
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> option) : base(option)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
