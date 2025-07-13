using Microsoft.EntityFrameworkCore;

namespace WordsAPI.Domain;

public class DataHelper
{
    public static async Task ManageDatabaseAsync(IServiceProvider svcProvider)
    {
        var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();

        await dbContextSvc.Database.MigrateAsync();
    }
}