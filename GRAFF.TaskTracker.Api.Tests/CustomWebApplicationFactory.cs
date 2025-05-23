using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GRAFF.TaskTracker.Api.Data; // Your DbContext namespace
using System;
using System.Linq;

namespace GRAFF.TaskTracker.Api.Tests
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's DbContext registration.
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TaskTrackerDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add DbContext using an in-memory database for testing.
                // Using a unique name for each test run by appending a Guid
                services.AddDbContext<TaskTrackerDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"InMemoryDbForTesting-{Guid.NewGuid()}");
                });

                // Optionally, re-seed data or ensure schema is created
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<TaskTrackerDbContext>();
                    // db.Database.EnsureDeleted(); // Ensure a clean state if DB was persisted (not typical for InMemory)
                    db.Database.EnsureCreated(); // Ensure schema is created for in-memory

                    // TODO: Seed data if necessary for your tests
                    // SeedData(db); 
                }
            });

            builder.UseEnvironment("Development"); // Or a specific "Testing" environment
        }

        // Example SeedData method (optional)
        // private static void SeedData(TaskTrackerDbContext context)
        // {
        //     // Add any test data you need for all tests using this factory
        //     // context.Users.Add(...);
        //     // context.SaveChanges();
        // }
    }
}
