using Projekt.Model.DataModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Projekt.DAL
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, int>
    {
        // table properties
        public virtual DbSet<Character> Characters { get; set; } = null!;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            //configuration commands            
            optionsBuilder.UseLazyLoadingProxies(); //enable lazy loading proxies
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Fluent API commands
           modelBuilder.Entity<Character>()
            .HasOne(c => c.User)
            .WithMany(u => u.Characters)
            .HasForeignKey(c=>c.UserId);

            modelBuilder.Entity<CharacterItem>()
                .HasKey(ci => new { ci.CharacterId, ci.ItemId });

            modelBuilder.Entity<CharacterItem>()
                .HasOne(ci => ci.Character)
                .WithMany(c => c.CharacterItems)
                .HasForeignKey(ci => ci.CharacterId);

            modelBuilder.Entity<CharacterItem>()
                .HasOne(ci => ci.Item)
                .WithMany(i => i.CharacterItems)
                .HasForeignKey(ci => ci.ItemId);
        }
    }
}
