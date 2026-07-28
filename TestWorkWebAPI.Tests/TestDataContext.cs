using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace TestWorkWebAPI.Tests;

public class TestDataContext : DataContext
{
    public TestDataContext(DbContextOptions<TestDataContext> options)
        : base(options)  // передаём options в базовый класс
    {
    }
}