using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ThirdPartyAddress : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid ThirdPartyId { get; set; }

    public string AddressType { get; set; } = "Principal";
    public string Street { get; set; } = string.Empty;
    public string ExteriorNumber { get; set; } = string.Empty;
    public string InteriorNumber { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;

    public Guid? CountryId { get; set; }
    public Country? Country { get; set; }

    public Guid? StateId { get; set; }
    public State? State { get; set; }

    public Guid? CityId { get; set; }
    public City? City { get; set; }

    public string Reference { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
