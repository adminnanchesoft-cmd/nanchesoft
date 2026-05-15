using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class CfdiSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        // Seeder base intencionalmente ligero.
        // Aquí se podrán crear configuraciones demo CFDI una vez que el modelo
        // se integre al DbContext real del proyecto.
        await Task.CompletedTask;
    }
}
