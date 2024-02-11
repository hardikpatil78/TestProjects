using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace TestWebApiDocker.Model
{
    public class MathOperationDb : DbContext
    {
        public MathOperationDb(DbContextOptions<MathOperationDb> options) : base(options) { }

        public DbSet<Sum> tblSum { get; set; }
    }
}
