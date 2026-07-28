using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DataAccess;

public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var startupProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../TestWorkWebAPI");
        Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine($"Looking for appsettings.json in: {startupProjectPath}");
        Console.WriteLine($"File exists: {File.Exists(Path.Combine(startupProjectPath, "appsettings.json"))}");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(startupProjectPath)
            .AddJsonFile("appsettings.json", optional: false)  // optional: false выбросит исключение при отсутствии
            .Build();

        var connectionString = configuration.GetConnectionString("WebApiDataBase");
        Console.WriteLine($"Connection string: {connectionString ?? "NULL"}");

        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseNpgsql<DataContext>(connectionString);
        return new DataContext(optionsBuilder.Options);
    }
}