namespace AvaChat.Client.Models;

public class ClientDbContext(DbContextOptions<ClientDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=AvaChat.Client.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.UserId).IsUnique();

        modelBuilder
            .Entity<Friendship>()
            .HasIndex(f => new { f.UserId, f.FriendUserId })
            .IsUnique();

        modelBuilder
            .Entity<Message>()
            .HasIndex(m => new
            {
                m.SenderId,
                m.ReceiverId,
                m.Timestamp,
            });
    }
}
