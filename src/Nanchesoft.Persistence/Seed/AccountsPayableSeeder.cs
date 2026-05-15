using System.Threading.Tasks;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class AccountsPayableSeeder
{
    public static Task SeedAsync(NanchesoftDbContext dbContext)
    {
        // Base hook para sembrar dashboard, navegación o datos demo de CxP.
        return Task.CompletedTask;
    }
}
