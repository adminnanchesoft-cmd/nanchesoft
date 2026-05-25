using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ThemeEndpoints
{
    public static IEndpointRouteBuilder MapThemeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/user-theme").WithTags("Theme");

        group.MapGet("/{userId:guid}", GetAsync);
        group.MapPut("/{userId:guid}", PutAsync);

        return app;
    }

    private static async Task<IResult> GetAsync(Guid userId, NanchesoftDbContext db)
    {
        var pref = await db.UserThemePreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (pref is null)
        {
            return Results.Ok(new UserThemePreferenceDto
            {
                UserId = userId,
                ThemeName = "dx.light",
                AccentColor = "#3557ff",
                SecondaryColor = string.Empty,
                BackgroundColor = string.Empty,
                AutoModeEnabled = false,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return Results.Ok(new UserThemePreferenceDto
        {
            UserId = pref.UserId,
            ThemeName = pref.ThemeName,
            AccentColor = pref.AccentColor,
            SecondaryColor = pref.SecondaryColor,
            BackgroundColor = pref.BackgroundColor,
            AutoModeEnabled = pref.AutoModeEnabled,
            UpdatedAt = pref.UpdatedAt
        });
    }

    private static async Task<IResult> PutAsync(Guid userId, UserThemePreferenceDto dto, NanchesoftDbContext db)
    {
        var pref = await db.UserThemePreferences.FirstOrDefaultAsync(x => x.UserId == userId);

        if (pref is null)
        {
            pref = new UserThemePreference { UserId = userId };
            db.UserThemePreferences.Add(pref);
        }

        pref.ThemeName = dto.ThemeName ?? "dx.light";
        pref.AccentColor = dto.AccentColor ?? "#3557ff";
        pref.SecondaryColor = dto.SecondaryColor ?? string.Empty;
        pref.BackgroundColor = dto.BackgroundColor ?? string.Empty;
        pref.AutoModeEnabled = dto.AutoModeEnabled;
        pref.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new UserThemePreferenceDto
        {
            UserId = pref.UserId,
            ThemeName = pref.ThemeName,
            AccentColor = pref.AccentColor,
            SecondaryColor = pref.SecondaryColor,
            BackgroundColor = pref.BackgroundColor,
            AutoModeEnabled = pref.AutoModeEnabled,
            UpdatedAt = pref.UpdatedAt
        });
    }
}

public sealed class UserThemePreferenceDto
{
    public Guid UserId { get; set; }
    public string? ThemeName { get; set; }
    public string? AccentColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? BackgroundColor { get; set; }
    public bool AutoModeEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }
}
