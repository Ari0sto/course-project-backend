using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechStore.Entities;

namespace TechStore.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // 1. Новая таблица с машинами
        public DbSet<Car> Cars { get; set; }

        // 2. Логи действий админа
        public DbSet<ActionLog> ActionLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 3. Настройка цен для Car
            builder.Entity<Car>().Property(c => c.PriceUSD).HasColumnType("decimal(18,2)");
            builder.Entity<Car>().Property(c => c.PriceUAH).HasColumnType("decimal(18,2)");
        }
    }
}