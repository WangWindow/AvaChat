namespace AvaChat.Client.Models;

public class ClientDbContext(DbContextOptions<ClientDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Friendship> Friendships { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<ClientSetting> ClientSettings { get; set; } = null!;
    public DbSet<LoginInfo> LoginInfos { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbDir = Path.Combine(appData, "AvaChat");
            Directory.CreateDirectory(dbDir);
            var dbPath = Path.Combine(dbDir, "AvaChat.Client.db");
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

        modelBuilder
            .Entity<Message>()
            .HasIndex(m => new
            {
                m.SenderId,
                m.ReceiverId,
                m.Timestamp,
            });

        modelBuilder.Entity<ClientSetting>();
        modelBuilder.Entity<LoginInfo>();
    }
}
