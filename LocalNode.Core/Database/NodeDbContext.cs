using Microsoft.EntityFrameworkCore;

namespace LocalNode.Core.Database;

//REIKALAVIMAS
public class DownloadLog
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime DownloadedAt { get; set; }
}

public class NodeDbContext : DbContext
{
    public DbSet<DownloadLog> DownloadLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=localnode.db");
    }
}