using Nanchesoft.Web.Services;

namespace Nanchesoft.Web.State;

public sealed class ShellState
{
    private readonly HashSet<string> _favorites = new(StringComparer.OrdinalIgnoreCase)
    {
        "/dashboard",
        "/purchases/orders",
        "/inventory/kardex",
        "/sales/invoices"
    };

    private readonly List<string> _recentRoutes = new();

    public event Action? Changed;

    private string _searchText = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_searchText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _searchText = normalized;
            Notify();
        }
    }

    public void ToggleFavorite(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return;
        }

        if (!_favorites.Add(route))
        {
            _favorites.Remove(route);
        }

        Notify();
    }

    public bool IsFavorite(string route)
        => !string.IsNullOrWhiteSpace(route) && _favorites.Contains(route);

    public void Remember(string route, NavigationService navigation)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return;
        }

        var normalized = navigation.Normalize(route);
        _recentRoutes.RemoveAll(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
        _recentRoutes.Insert(0, normalized);

        if (_recentRoutes.Count > 10)
        {
            _recentRoutes.RemoveRange(10, _recentRoutes.Count - 10);
        }

        if (navigation.GetActiveItem(normalized) is not null)
        {
            Notify();
        }
    }

    public IEnumerable<NavigationMenuItem> GetFavoriteItems(NavigationService navigation)
        => _favorites
            .Select(route => navigation.GetActiveItem(route))
            .Where(item => item is not null)
            .Cast<NavigationMenuItem>();

    public IEnumerable<NavigationMenuItem> GetRecentItems(NavigationService navigation)
        => _recentRoutes
            .Select(route => navigation.GetActiveItem(route))
            .Where(item => item is not null)
            .Cast<NavigationMenuItem>();

    private void Notify() => Changed?.Invoke();
}
