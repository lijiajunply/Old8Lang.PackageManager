using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Old8Lang.PackageManager.Server.Data;

namespace Old8Lang.PackageManager.Server;

public class DesignTimeDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<PackageManagerDbContext>
{
    public PackageManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PackageManagerDbContext>();
        optionsBuilder.UseSqlite("Data Source=packages.db");
        
        return new PackageManagerDbContext(optionsBuilder.Options);
    }
}