using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class State : BaseEntity
{
    public Guid CountryId { get; set; }
    public Country? Country { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<City> Cities { get; set; } = new List<City>();
}
