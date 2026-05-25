namespace Nanchesoft.Web.State;

public sealed class ThemeState
{
    public event Action? OnChange;

    public string ThemeName { get; private set; } = "dx.light";
    public string AccentColor { get; private set; } = "#3557ff";
    public string SecondaryColor { get; private set; } = string.Empty;
    public string BackgroundColor { get; private set; } = string.Empty;
    public bool AutoModeEnabled { get; private set; }
    public bool IsLoaded { get; private set; }

    public void Apply(string themeName, string accentColor, string secondaryColor, string backgroundColor, bool autoModeEnabled)
    {
        ThemeName = themeName;
        AccentColor = accentColor;
        SecondaryColor = secondaryColor;
        BackgroundColor = backgroundColor;
        AutoModeEnabled = autoModeEnabled;
        IsLoaded = true;
        OnChange?.Invoke();
    }

    public void SetAutoMode(bool enabled)
    {
        AutoModeEnabled = enabled;
        OnChange?.Invoke();
    }
}
