using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace TestWorkWebAPI;

public class AppConfig
{
    private readonly IConfiguration _configuration;
    
    public AppConfig(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString(string name)
    {
        return _configuration.GetConnectionString(name);
    }
}