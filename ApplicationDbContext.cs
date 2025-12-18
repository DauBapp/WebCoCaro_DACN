using Microsoft.EntityFrameworkCore;
using WebChoiCoCaro.Models;

namespace WebChoiCoCaro.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<GameHistory> GameHistories { get; set; }
        public DbSet<GameMove> GameMoves { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicit table mapping to avoid naming mismatch
            modelBuilder.Entity<GameHistory>().ToTable("GameHistories");
            modelBuilder.Entity<GameMove>().ToTable("GameMoves");

            // optional: configure keys/relations if needed
            modelBuilder.Entity<GameMove>()
                .HasOne<GameHistory>()
                .WithMany()
                .HasForeignKey(m => m.GameHistoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}