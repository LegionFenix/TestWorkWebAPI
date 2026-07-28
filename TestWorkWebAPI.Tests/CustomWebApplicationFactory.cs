using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DataAccess;

namespace TestWorkWebAPI.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 1. Удаляем все регистрации, связанные с DataContext
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<DataContext>) ||
                            d.ServiceType == typeof(DbContextOptions) ||
                            d.ServiceType == typeof(DataContext))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            // 2. Регистрируем TestDataContext как реализацию DataContext
            services.AddScoped<DataContext, TestDataContext>(provider =>
            {
                // Создаём options для TestDataContext с InMemory
                var optionsBuilder = new DbContextOptionsBuilder<TestDataContext>();
                optionsBuilder.UseInMemoryDatabase("TestDb");
                var options = optionsBuilder.Options;
                return new TestDataContext(options);
            });

            // 3. Создаём контекст и гарантируем создание схемы
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            dbContext.Database.EnsureCreated();
        });
    }
}