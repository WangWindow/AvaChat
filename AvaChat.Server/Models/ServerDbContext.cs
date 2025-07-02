namespace AvaChat.Server.Models;

public class ServerDbContext(DbContextOptions<ServerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Friendship> Friendships { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<FriendRequestEntity> FriendRequests { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // optionsBuilder.UseSqlite("Data Source=AvaChat.Server.db");
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbDir = Path.Combine(appData, "AvaChat");
            Directory.CreateDirectory(dbDir);
            var dbPath = Path.Combine(dbDir, "AvaChat.Server.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<User>()
            .HasIndex(u => u.UserId)
            .IsUnique();

        modelBuilder
            .Entity<Friendship>()
            .HasIndex(f => new { f.UserId, f.FriendUserId })
            .IsUnique();
    }
}
