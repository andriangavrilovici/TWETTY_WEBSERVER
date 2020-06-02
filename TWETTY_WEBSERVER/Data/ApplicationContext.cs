using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TWETTY_WEBSERVER
{
    public class ApplicationContext : IdentityDbContext<Users>
    {
        public DbSet<FriendsDataModel> Friends{ get; set; }
        public DbSet<MessagesDataModel> Messages { get; set; }
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fluent API
            modelBuilder.Entity<FriendsDataModel>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<MessagesDataModel>().HasIndex(a => a.Id).IsUnique();
        }
    }
}
