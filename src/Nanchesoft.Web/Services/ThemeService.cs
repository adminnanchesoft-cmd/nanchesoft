using System.Net.Http.Json;
using Microsoft.JSInterop;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services;

public sealed class ThemeService
{
    // Holiday palettes: (startMonth, startDay, endMonth, endDay, accent, secondary, name)
    private static readonly (int M1, int D1, int M2, int D2, string Accent, string Secondary, string Name)[] _holidays =
    [
        (12,  1, 12, 25, "#C62828", "#2E7D32", "Navidad"),
        (12, 26, 12, 31, "#B8860B", "#283593", "Año Nuevo"),
        ( 1,  1,  1,  5, "#B8860B", "#283593", "Año Nuevo"),
        ( 2, 10,  2, 14, "#E91E63", "#880E4F", "San Valentín"),
        (10, 31, 11,  2, "#FF6F00", "#6A1B9A", "Día de Muertos"),
        ( 9, 15,  9, 16, "#1B5E20", "#B71C1C", "Independencia MX"),
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthState _authState;
    private readonly ThemeState _themeState;
    private readonly IJSRuntime _js;

    public ThemeService(
        IHttpClientFactory httpClientFactory,
        AuthState authState,
        ThemeState themeState,
        IJSRuntime js)
    {
        _httpClientFactory = httpClientFactory;
        _authState = authState;
        _themeState = themeState;
        _js = js;
    }

    // Fetch preferences from API and update ThemeState + apply to document.
    // Must be called from within a Blazor component lifecycle method (OnAfterRenderAsync)
    // so that the IJSRuntime call runs on the SignalR circuit context.
    public async Task LoadAsync()
    {
        if (!_authState.IsAuthenticated || _authState.UserId is null)
            return;

        try
        {
            var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
            var dto = await client.GetFromJsonAsync<ThemePreferenceDto>($"/api/user-theme/{_authState.UserId}");
            if (dto is null) return;

            _themeState.Apply(dto.ThemeName, dto.AccentColor, dto.SecondaryColor, dto.BackgroundColor, dto.AutoModeEnabled);
        }
        catch
        {
            // Non-critical — keep defaults
        }

        // Apply to document separately so callers can await this part on the circuit
        await ApplyToDocumentAsync();
    }

    // Save and immediately apply.
    public async Task SaveAsync(string themeName, string accentColor, string secondaryColor, string backgroundColor, bool autoModeEnabled)
    {
        if (_authState.UserId is null) return;

        _themeState.Apply(themeName, accentColor, secondaryColor, backgroundColor, autoModeEnabled);
        await ApplyToDocumentAsync();

        try
        {
            var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
            await client.PutAsJsonAsync($"/api/user-theme/{_authState.UserId}", new ThemePreferenceDto
            {
                UserId = _authState.UserId.Value,
                ThemeName = themeName,
                AccentColor = accentColor,
                SecondaryColor = secondaryColor,
                BackgroundColor = backgroundColor,
                AutoModeEnabled = autoModeEnabled
            });
        }
        catch
        {
            // Best-effort
        }
    }

    // Apply the current ThemeState to the document via nsTheme.apply().
    // Call this from a component lifecycle method to ensure circuit context.
    public async Task ApplyToDocumentAsync()
    {
        var (themeName, accent, secondary) = Resolve();
        try
        {
            await _js.InvokeVoidAsync("nsTheme.apply",
                themeName, accent, secondary,
                _themeState.BackgroundColor,
                _themeState.AutoModeEnabled);
        }
        catch
        {
            // JS not ready yet (prerender / SSR context) — the localStorage anti-FOUC
            // script in App.razor handles the initial render.
        }
    }

    private (string ThemeName, string Accent, string Secondary) Resolve()
    {
        if (!_themeState.AutoModeEnabled)
            return (_themeState.ThemeName, _themeState.AccentColor, _themeState.SecondaryColor);

        var today = DateTime.Today;

        foreach (var h in _holidays)
        {
            if (IsInRange(today, h.M1, h.D1, h.M2, h.D2))
                return (_themeState.ThemeName, h.Accent, h.Secondary);
        }

        var (_, accent, secondary) = GetSeasonalPalette(today);
        return (_themeState.ThemeName, accent, secondary);
    }

    private static bool IsInRange(DateTime date, int m1, int d1, int m2, int d2)
    {
        int m = date.Month, d = date.Day;
        if (m1 == m2)
            return m == m1 && d >= d1 && d <= d2;
        if (m1 < m2)
            return (m == m1 && d >= d1) || (m > m1 && m < m2) || (m == m2 && d <= d2);
        // Wraps year (e.g. Dec 26 – Jan 5)
        return (m == m1 && d >= d1) || (m == m2 && d <= d2);
    }

    private static (string Key, string Accent, string Secondary) GetSeasonalPalette(DateTime date)
    {
        int m = date.Month, d = date.Day;
        if ((m == 3 && d >= 21) || (m is 4 or 5) || (m == 6 && d <= 20))
            return ("spring", "#7CB342", "#F06292");
        if ((m == 6 && d >= 21) || (m is 7 or 8) || (m == 9 && d <= 22))
            return ("summer", "#00ACC1", "#1976D2");
        if ((m == 9 && d >= 23) || m == 10 || m == 11)
            return ("autumn", "#EF6C00", "#8D6E63");
        return ("winter", "#1E88E5", "#546E7A");
    }

    // Returns a human-readable label for the palette active today.
    public string GetActivePaletteName()
    {
        var today = DateTime.Today;
        foreach (var h in _holidays)
        {
            if (IsInRange(today, h.M1, h.D1, h.M2, h.D2))
                return h.Name;
        }
        var (key, _, _) = GetSeasonalPalette(today);
        return key switch
        {
            "spring" => "Primavera",
            "summer" => "Verano",
            "autumn" => "Otoño",
            "winter" => "Invierno",
            _ => "Predeterminado"
        };
    }

    public (string Accent, string Secondary) GetCurrentColors()
    {
        var (_, accent, secondary) = Resolve();
        return (accent, secondary);
    }
}

public sealed class ThemePreferenceDto
{
    public Guid UserId { get; set; }
    public string ThemeName { get; set; } = "dx.light";
    public string AccentColor { get; set; } = "#3557ff";
    public string SecondaryColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public bool AutoModeEnabled { get; set; }
}
