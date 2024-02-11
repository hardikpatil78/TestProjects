using Microsoft.EntityFrameworkCore;

namespace TestWebApp.Data
{
    public class TestWebAppDbContext: DbContext
    {
        public TestWebAppDbContext(DbContextOptions<TestWebAppDbContext> options) : base(options) { }

        public DbSet<tblDrive> tblDrive { get; set; }
        public DbSet<tblFile> tblFile { get; set; }
    }
}
