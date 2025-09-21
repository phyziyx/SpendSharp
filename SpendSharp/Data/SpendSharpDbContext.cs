using Microsoft.EntityFrameworkCore;
using SpendSharp.Models;

namespace SpendSharp.Data
{
    public class SpendSharpDbContext : DbContext
    {
        public DbSet<Expense> Expenses { get; set; }

        public SpendSharpDbContext(DbContextOptions<SpendSharpDbContext> options) : base(options)
        {
            
        }
    }
}
