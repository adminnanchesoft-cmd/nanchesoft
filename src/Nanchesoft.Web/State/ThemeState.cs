namespace Nanchesoft.Web.State;

public sealed class ThemeState
{
    public string ThemeName { get; private set; } = "dx.light";
    public string AccentColor { get; private set; } = "#3557ff";
    public string SecondaryColor { get; private set; } = string.Empty;
    public string BackgroundColor { get; private set; } = string.Empty;
    public bool AutoModeEnabled { get; private set; }
    public bool IsLoaded { get; private set; }

    public event Action? OnChange;

    public void Apply(string themeName, string accent, string secondary, string background, bool autoMode)
    {
        ThemeName = themeName;
        AccentColor = accent;
        SecondaryColor = secondary;
        BackgroundColor = background;
        AutoModeEnabled = autoMode;
        IsLoaded = true;
        OnChange?.Invoke();
    }

    public void SetAutoMode(bool enabled)
    {
        AutoModeEnabled = enabled;
        OnChange?.Invoke();
    }
}
