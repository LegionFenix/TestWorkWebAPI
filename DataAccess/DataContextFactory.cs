using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


namespace DataAccess;

public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        // Путь к папке стартового проекта (обычно на уровень выше)
        var startupProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../TestWorkWebAPI");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(startupProjectPath)
            .AddJsonFile("appsettings.json")
            .Build();

        // Создаём экземпляр AppConfig (без DI)
        var appConfig = new TestWorkWebAPI.AppConfig(configuration);
        var connectionString = appConfig.GetConnectionString("WebApiDataBase");

        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DataContext(optionsBuilder.Options);
    }
}