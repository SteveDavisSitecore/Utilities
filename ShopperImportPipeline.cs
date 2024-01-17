using OrderCloud.SDK;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Utilities
{
    public class ShopperImportPipeline
    {
        private readonly IOrderCloudClient _ocAdmin;
        private readonly AppSettings _settings;
        public ShopperImportPipeline(IOrderCloudClient ocAdmin, AppSettings settings)
        {
            _ocAdmin = ocAdmin;
            _settings = settings;
        }

        public async Task RunAsync()
        {
            var catalog = await _ocAdmin.Catalogs.SaveAsync(_settings.CatalogID.ToSafeID(), new Catalog()
            {
                Active = true,
                Description = _settings.CatalogID.ToSafeID(),
                ID = _settings.CatalogID.ToSafeID(),
                Name = _settings.CatalogID.ToSafeID()
            });
            Console.WriteLine($"Catalog {catalog.Name} PUT");

            var category = await _ocAdmin.Categories.SaveAsync(_settings.CatalogID.ToSafeID(), _settings.CategoryID.ToSafeID(), new Category()
            {
                Active = true,
                Description = _settings.CategoryID.ToSafeID(),
                ID = _settings.CategoryID.ToSafeID(),
                Name = _settings.CategoryID.ToSafeID()
            });
            Console.WriteLine($"Category {category.Name} PUT");

            var buyer = await _ocAdmin.Buyers.SaveAsync(_settings.BuyerID.ToSafeID(), new Buyer()
            {
                ID = _settings.BuyerID.ToSafeID(),
                Name = _settings.BuyerID,
                DefaultCatalogID = catalog.ID,
                Active = true
            });
            Console.WriteLine($"Buyer {buyer.Name} PUT");

            var user = await _ocAdmin.Users.SaveAsync(_settings.BuyerID, _settings.AnonUserID.ToSafeID(), new User()
            {
                ID = _settings.AnonUserID.ToSafeID(),
                Username = _settings.AnonUserID.ToSafeID(),
                Active = true,
                Email = _settings.UserEmail,
                FirstName = "General",
                LastName = "Public"
            });
            Console.WriteLine($"User {user.ID} PUT");

            //var anonClient = await _ocAdmin.ApiClients.ListAsync(filters: new { IsAnonBuyer = true });
            //var apiClient = anonClient.Items.Last();
            //if (anonClient.Items.Count == 0)
            //{
            var apiClient = await _ocAdmin.ApiClients.CreateAsync(new ApiClient()
            {
                AccessTokenDuration = 600,
                RefreshTokenDuration = 43200,
                AllowAnyBuyer = true,
                AllowSeller = true,
                AllowAnySupplier = true,
                DefaultContextUserName = user.ID,
                IsAnonBuyer = true,
                Active = true,
                AppName = "Shopper",
                MinimumRequiredRoles = new List<ApiRole> { ApiRole.Shopper }
            });
            Console.WriteLine($"API Client {apiClient.ID} created");

                await _ocAdmin.ApiClients.SaveAssignmentAsync(new ApiClientAssignment()
                {
                    ApiClientID = apiClient.ID,
                    BuyerID = buyer.ID
                });
                Console.WriteLine($"API Client Assignment to Buyer {apiClient.ID} > {buyer.Name} created");

                var securityProfile = await _ocAdmin.SecurityProfiles.SaveAsync(_settings.SecurityProfileID.ToSafeID(), new SecurityProfile()
                {
                    ID = _settings.SecurityProfileID.ToSafeID(),
                    Name = "ShopperProfile",
                    Roles = new List<ApiRole> { ApiRole.Shopper }
                });
                Console.WriteLine($"Security Profile {securityProfile.ID} created");

                await _ocAdmin.SecurityProfiles.SaveAssignmentAsync(new SecurityProfileAssignment()
                {
                    BuyerID = buyer.ID,
                    SecurityProfileID = securityProfile.ID
                });
                Console.WriteLine($"Security Profile Assignment to Buyyer {securityProfile.ID} > {buyer.ID} created");
            //}
            //else
            //{
            //    Console.WriteLine($"Anonymous Client ID already exists");
            //}
        }
    }
}