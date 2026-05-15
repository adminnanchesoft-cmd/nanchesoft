namespace Nanchesoft.Web.State;

public static class ShellMemoryStore
{
    private static readonly object Sync = new();
    private static readonly List<ShellBookmark> Favorites = new();
    private static readonly List<ShellBookmark> Recents = new();

    public static IReadOnlyList<ShellBookmark> GetFavorites()
    {
        lock (Sync)
        {
            return Favorites.ToList();
        }
    }

    public static IReadOnlyList<ShellBookmark> GetRecents()
    {
        lock (Sync)
        {
            return Recents.ToList();
        }
    }

    public static bool IsFavorite(string route)
    {
        lock (Sync)
        {
            return Favorites.Any(x => string.Equals(x.Route, route, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static void ToggleFavorite(string title, string route, string module)
    {
        if (string.IsNullOrWhiteSpace(route)) return;

        lock (Sync)
        {
            var existing = Favorites.FirstOrDefault(x => string.Equals(x.Route, route, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                Favorites.Remove(existing);
                return;
            }

            Favorites.Insert(0, new ShellBookmark(title, route, module));
            Trim(Favorites, 8);
        }
    }

    public static void Remember(string title, string route, string module)
    {
        if (string.IsNullOrWhiteSpace(route)) return;

        lock (Sync)
        {
            Recents.RemoveAll(x => string.Equals(x.Route, route, StringComparison.OrdinalIgnoreCase));
            Recents.Insert(0, new ShellBookmark(title, route, module));
            Trim(Recents, 10);
        }
    }

    private static void Trim(List<ShellBookmark> items, int max)
    {
        while (items.Count > max)
        {
            items.RemoveAt(items.Count - 1);
        }
    }
}

public sealed record ShellBookmark(string Title, string Route, string Module);
