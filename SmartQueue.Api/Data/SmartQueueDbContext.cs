using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Models;

namespace SmartQueue.Api.Data
{
    public class SmartQueueDbContext : IdentityDbContext<ApplicationUser>
    {
        public SmartQueueDbContext(DbContextOptions<SmartQueueDbContext> options)
            : base(options)
        {
        }

        public DbSet<Queue> Queues { get; set; } = null!;

        public DbSet<QueueTicket> QueueTickets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Queue>()
                .Property(q => q.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Entity<QueueTicket>()
                .Property(t => t.CustomerName)
                .HasMaxLength(100)
                .IsRequired();

       

            builder.Entity<QueueTicket>()
                .HasOne(t => t.Queue)
                .WithMany(q => q.Tickets)
                .HasForeignKey(t => t.QueueId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}