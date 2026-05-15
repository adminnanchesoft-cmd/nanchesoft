using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class ThirdPartiesProductsSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed-sprint4";

        var tenant = await dbContext.Tenants.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        if (tenant is null || company is null)
            return;

        var mxn = await dbContext.Currencies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Code == "MXN")
                  ?? await dbContext.Currencies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        var iva = await dbContext.Taxes.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var unit = await dbContext.Units.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var bank = await dbContext.Banks.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var country = await dbContext.Countries.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var state = await dbContext.States.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var city = await dbContext.Cities.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        var wholesalePriceList = await dbContext.ItemPriceLists.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "MAYOREO");
        if (wholesalePriceList is null)
        {
            wholesalePriceList = new ItemPriceList
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                CurrencyId = mxn?.Id,
                Code = "MAYOREO",
                Name = "Lista mayoreo",
                IsDefault = true,
                CreatedBy = seedUser
            };
            dbContext.ItemPriceLists.Add(wholesalePriceList);
            await dbContext.SaveChangesAsync();
        }

        var category = await dbContext.ItemCategories.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "ELEC");
        if (category is null)
        {
            category = new ItemCategory
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = "ELEC",
                Name = "Electrónica",
                CreatedBy = seedUser
            };
            dbContext.ItemCategories.Add(category);
            await dbContext.SaveChangesAsync();
        }

        var brand = await dbContext.ItemBrands.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "NAN");
        if (brand is null)
        {
            brand = new ItemBrand
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = "NAN",
                Name = "Nanchesoft",
                CreatedBy = seedUser
            };
            dbContext.ItemBrands.Add(brand);
            await dbContext.SaveChangesAsync();
        }

        var model = await dbContext.ItemModels.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "ERP-BOX");
        if (model is null)
        {
            model = new ItemModel
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = "ERP-BOX",
                Name = "ERP Box",
                CreatedBy = seedUser
            };
            dbContext.ItemModels.Add(model);
            await dbContext.SaveChangesAsync();
        }

        var item = await dbContext.Items.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "ERP-001");
        if (item is null)
        {
            item = new Item
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                CategoryId = category.Id,
                BrandId = brand.Id,
                ModelId = model.Id,
                UnitId = unit?.Id,
                TaxId = iva?.Id,
                CurrencyId = mxn?.Id,
                Code = "ERP-001",
                Barcode = "7500000000010",
                Name = "Licencia ERP Cloud",
                Description = "Producto demo de sprint 4.",
                ItemType = "Servicio",
                BasePrice = 15000m,
                BaseCost = 9000m,
                ManagesInventory = false,
                UsesLots = false,
                UsesSerials = false,
                IsSaleItem = true,
                IsPurchaseItem = false,
                CreatedBy = seedUser
            };
            dbContext.Items.Add(item);
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.ItemBarcodes.AnyAsync(x => x.ItemId == item.Id && x.Barcode == "7500000000010"))
        {
            dbContext.ItemBarcodes.Add(new ItemBarcode
            {
                ItemId = item.Id,
                Barcode = "7500000000010",
                IsPrimary = true,
                CreatedBy = seedUser
            });
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.ItemPriceListDetails.AnyAsync(x => x.PriceListId == wholesalePriceList.Id && x.ItemId == item.Id))
        {
            dbContext.ItemPriceListDetails.Add(new ItemPriceListDetail
            {
                PriceListId = wholesalePriceList.Id,
                ItemId = item.Id,
                Price = 14999m,
                ValidFrom = DateTime.UtcNow.Date,
                CreatedBy = seedUser
            });
            await dbContext.SaveChangesAsync();
        }

        var customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "CLI001");
        if (customer is null)
        {
            customer = new Customer
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                CurrencyId = mxn?.Id,
                PriceListId = wholesalePriceList.Id,
                Code = "CLI001",
                Name = "Cliente Demo",
                LegalName = "Cliente Demo SA de CV",
                TaxId = "XAXX010101000",
                Email = "cliente.demo@nanchesoft.com",
                Phone = "4771001000",
                CreditLimit = 50000m,
                PaymentTermDays = 30,
                CreatedBy = seedUser
            };
            dbContext.Customers.Add(customer);
            await dbContext.SaveChangesAsync();
        }

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "PROV001");
        if (supplier is null)
        {
            supplier = new Supplier
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                CurrencyId = mxn?.Id,
                Code = "PROV001",
                Name = "Proveedor Demo",
                LegalName = "Proveedor Demo SA de CV",
                TaxId = "XEXX010101000",
                Email = "proveedor.demo@nanchesoft.com",
                Phone = "4772002000",
                PaymentTermDays = 15,
                CreatedBy = seedUser
            };
            dbContext.Suppliers.Add(supplier);
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.ThirdPartyContacts.AnyAsync(x => x.ThirdPartyType == "customer" && x.ThirdPartyId == customer.Id))
        {
            dbContext.ThirdPartyContacts.Add(new ThirdPartyContact
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                ThirdPartyType = "customer",
                ThirdPartyId = customer.Id,
                Name = "María Comercial",
                Position = "Compras",
                Email = "maria@cliente-demo.com",
                Phone = "4773003000",
                Mobile = "4773003001",
                IsPrimary = true,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.ThirdPartyAddresses.AnyAsync(x => x.ThirdPartyType == "customer" && x.ThirdPartyId == customer.Id))
        {
            dbContext.ThirdPartyAddresses.Add(new ThirdPartyAddress
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                ThirdPartyType = "customer",
                ThirdPartyId = customer.Id,
                AddressType = "Fiscal",
                Street = "Boulevard Demo",
                ExteriorNumber = "123",
                InteriorNumber = "A",
                Neighborhood = "Centro",
                ZipCode = "37000",
                CountryId = country?.Id,
                StateId = state?.Id,
                CityId = city?.Id,
                Reference = "Frente a la plaza",
                IsPrimary = true,
                CreatedBy = seedUser
            });
        }

        if (bank is not null && !await dbContext.ThirdPartyBankAccounts.AnyAsync(x => x.ThirdPartyType == "supplier" && x.ThirdPartyId == supplier.Id))
        {
            dbContext.ThirdPartyBankAccounts.Add(new ThirdPartyBankAccount
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                ThirdPartyType = "supplier",
                ThirdPartyId = supplier.Id,
                BankId = bank.Id,
                CurrencyId = mxn?.Id,
                AccountHolder = "Proveedor Demo SA de CV",
                AccountNumber = "1234567890",
                Clabe = "012180001234567890",
                IsPrimary = true,
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
