using Microsoft.EntityFrameworkCore;
using OmniCore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Persistence.Seeders
{
    public static class RoleAndPermissionSeeder
    {
        public static void Seed(ModelBuilder builder)
        {
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var sellerRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var customerRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var supportRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");

            builder.Entity<Role>().HasData(
                new Role { Id = adminRoleId, Name = "Admin", IsDeleted = false, CreatedAt = DateTime.UtcNow },
                new Role { Id = sellerRoleId, Name = "Seller", IsDeleted = false, CreatedAt = DateTime.UtcNow },
                new Role { Id = customerRoleId, Name = "Customer", IsDeleted = false, CreatedAt = DateTime.UtcNow },
                new Role { Id = supportRoleId, Name = "Support", IsDeleted = false, CreatedAt = DateTime.UtcNow }
            );

            var permissions = new List<Permission>
            {
                new Permission { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Code = "CAN_VIEW_USERS", Description = "View all users", IsDeleted = false, CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Code = "CAN_EDIT_USERS", Description = "Edit users", IsDeleted = false, CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Code = "CAN_VIEW_PRODUCTS", Description = "View products", IsDeleted = false, CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), Code = "CAN_EDIT_PRODUCTS", Description = "Add or edit products", IsDeleted = false, CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), Code = "CAN_VIEW_ORDERS", Description = "View orders", IsDeleted = false, CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Code = "CAN_EDIT_ORDERS", Description = "Edit orders", IsDeleted = false, CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.Parse("11111111-aaaa-aaaa-aaaa-111111111111"), Code = "CAN_MANAGE_SETTINGS", Description = "Manage platform settings", IsDeleted = false, CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.Parse("22222222-bbbb-bbbb-bbbb-222222222222"), Code = "CAN_VIEW_REPORTS", Description = "View analytics reports", IsDeleted = false, CreatedAt = DateTime.UtcNow }
            };

            builder.Entity<Permission>().HasData(permissions);

            builder.Entity<RolePermission>().HasData(

                new { RoleId = adminRoleId, PermissionId = permissions[0].Id },
                new { RoleId = adminRoleId, PermissionId = permissions[1].Id },
                new { RoleId = adminRoleId, PermissionId = permissions[2].Id },
                new { RoleId = adminRoleId, PermissionId = permissions[3].Id },
                new { RoleId = adminRoleId, PermissionId = permissions[4].Id },
                new { RoleId = adminRoleId, PermissionId = permissions[5].Id },
                new { RoleId = adminRoleId, PermissionId = permissions[6].Id },
                new { RoleId = adminRoleId, PermissionId = permissions[7].Id },

                new { RoleId = sellerRoleId, PermissionId = permissions[2].Id },
                new { RoleId = sellerRoleId, PermissionId = permissions[3].Id },
                new { RoleId = sellerRoleId, PermissionId = permissions[4].Id },
                new { RoleId = sellerRoleId, PermissionId = permissions[5].Id },

                new { RoleId = customerRoleId, PermissionId = permissions[2].Id },
                new { RoleId = customerRoleId, PermissionId = permissions[4].Id },

                new { RoleId = supportRoleId, PermissionId = permissions[0].Id },
                new { RoleId = supportRoleId, PermissionId = permissions[2].Id },
                new { RoleId = supportRoleId, PermissionId = permissions[4].Id }
            );
        }
    }
}
