using Microsoft.EntityFrameworkCore;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync()
    {
        using var context = new BrowserDbContext();
        await context.Database.EnsureCreatedAsync();
        
        if (!await context.EnvironmentGroups.AnyAsync())
        {
            context.EnvironmentGroups.AddRange(
                new EnvironmentGroup { Name = "默认分组", Color = "#667eea", SortOrder = 0 },
                new EnvironmentGroup { Name = "工作", Color = "#00d4ff", SortOrder = 1 },
                new EnvironmentGroup { Name = "社交", Color = "#f093fb", SortOrder = 2 }
            );
            await context.SaveChangesAsync();
        }
    }
}
