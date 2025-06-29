using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AvaChat.Client.Models;

public class ClientDbContextFactory : IDesignTimeDbContextFactory<ClientDbContext>
{
    public ClientDbContext CreateDbContext(string[] args)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbDir = Path.Combine(appData, "AvaChat");
        Directory.CreateDirectory(dbDir);
        var dbPath = Path.Combine(dbDir, "AvaChat.Client.db");
        var optionsBuilder = new DbContextOptionsBuilder<ClientDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new ClientDbContext(optionsBuilder.Options);
    }
}
