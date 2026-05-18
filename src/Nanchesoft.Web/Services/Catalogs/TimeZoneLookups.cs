namespace Nanchesoft.Web.Services.Catalogs;

public static class TimeZoneLookups
{
    private static List<CatalogLookupItem>? _cache;

    public static List<CatalogLookupItem> GetItems()
    {
        if (_cache is not null) return _cache;

        _cache = TimeZoneInfo.GetSystemTimeZones()
            .OrderBy(tz => tz.BaseUtcOffset)
            .ThenBy(tz => tz.Id)
            .Select(tz =>
            {
                var offset = tz.BaseUtcOffset.Duration();
                var sign = tz.BaseUtcOffset >= TimeSpan.Zero ? "+" : "-";
                var label = $"(UTC{sign}{(int)offset.TotalHours:D2}:{offset.Minutes:D2}) {tz.Id}";
                return new CatalogLookupItem { Id = tz.Id, Name = label };
            })
            .ToList();

        return _cache;
    }

    public static bool IsValid(string? id)
        => !string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id.Trim(), out _);
}
