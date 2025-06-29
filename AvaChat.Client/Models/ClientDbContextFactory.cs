using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AvaChat.Client.Models;

public class ClientDbContextFactory : IDesignTimeDbContextFactory<ClientDbContext>
{
    public ClientDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClientDbContext>();
        optionsBuilder.UseSqlite("Data Source=AvaChat.Client.db");
        return new ClientDbContext(optionsBuilder.Options);
    }
}
