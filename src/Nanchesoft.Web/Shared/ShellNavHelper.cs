using Nanchesoft.Web.Services;

namespace Nanchesoft.Web.Shared;

public static class ShellNavHelper
{
    public static string NormalizeRoute(string? route)
    {
        if (string.IsNullOrWhiteSpace(route)) return "dashboard";
        return route.Trim().TrimStart('/');
    }

    public static IReadOnlyList<NavigationMenuGroup> GetGroups(NavigationService navigationService)
        => navigationService.GetMenu() ?? new List<NavigationMenuGroup>();

    public static NavigationMenuGroup? GetActiveGroup(NavigationService navigationService, string route)
    {
        var normalized = NormalizeRoute(route);

        var active = navigationService.GetActiveGroup(normalized);
        if (active is not null)
        {
            return active;
        }

        return GetGroups(navigationService)
            .FirstOrDefault(group => group.Items.Any(item => NormalizeRoute(item.Route) == normalized));
    }

    public static NavigationMenuItem? GetActiveItem(NavigationService navigationService, string route)
    {
        var normalized = NormalizeRoute(route);

        var active = navigationService.GetActiveItem(normalized);
        if (active is not null)
        {
            return active;
        }

        return GetGroups(navigationService)
            .SelectMany(group => group.Items)
            .FirstOrDefault(item => NormalizeRoute(item.Route) == normalized);
    }

    public static string GetGroupRoute(NavigationMenuGroup group)
    {
        var first = group.Items.FirstOrDefault();
        return NormalizeRoute(first?.Route);
    }

    public static string GetModuleLabel(NavigationMenuGroup? group)
    {
        return group?.Title ?? "Inicio";
    }

    public static IReadOnlyList<NavigationMenuItem> Search(NavigationService navigationService, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<NavigationMenuItem>();
        }

        return navigationService.Search(text);
    }

    public static IReadOnlyList<(string Title, string Route)> BuildBreadcrumb(NavigationService navigationService, string route)
    {
        var normalized = NormalizeRoute(route);
        var group = GetActiveGroup(navigationService, normalized);
        var item = GetActiveItem(navigationService, normalized);

        var parts = new List<(string Title, string Route)>
        {
            ("Inicio", "dashboard")
        };

        // Skip the group when it IS the home group (route resolves to "dashboard")
        // This avoids "Inicio > Inicio > ..." duplication caused by the NavigationService fallback
        var groupRoute = group is not null ? GetGroupRoute(group) : null;
        if (group is not null && groupRoute != "dashboard")
        {
            parts.Add((group.Title, groupRoute!));
        }

        // Add item only when it has a title distinct from the last breadcrumb already added
        if (item is not null && item.Title != parts[^1].Title)
        {
            parts.Add((item.Title, normalized));
        }

        return parts;
    }

    public static bool IsControlCenterRoute(string route)
    {
        var normalized = NormalizeRoute(route);
        return normalized is "" or "dashboard" or "/";
    }

    public static string GetModuleShortCode(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "NS";
        var pieces = title.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pieces.Length == 1) return pieces[0][..Math.Min(2, pieces[0].Length)].ToUpperInvariant();
        return string.Concat(pieces.Take(2).Select(x => char.ToUpperInvariant(x[0])));
    }
}
