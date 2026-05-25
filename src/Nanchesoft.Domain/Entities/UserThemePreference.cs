namespace Nanchesoft.Domain.Entities;

public sealed class UserThemePreference
{
    public Guid UserId { get; set; }

    // "dx.light" | "dx.dark" | "dx.fluent.saas.light" | etc.
    public string ThemeName { get; set; } = "dx.light";

    public string AccentColor { get; set; } = "#3557ff";
    public string SecondaryColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;

    public bool AutoModeEnabled { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
