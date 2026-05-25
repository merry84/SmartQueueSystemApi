using Microsoft.AspNetCore.Identity;
using SmartQueue.Api.Models;

namespace SmartQueue.Api.Data.Seed
{
    public static class UserSeeder
    {
        public static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
        {
            await SeedUserAsync(userManager, "admin@smartqueue.com", "Admin123!", "Admin");
            await SeedUserAsync(userManager, "operator@smartqueue.com", "Operator123!", "Operator");
        }

        private static async Task SeedUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join("; ", createResult.Errors.Select(e => e.Description)));
                }
            }

            user.EmailConfirmed = true;
            user.UserName = email;
            user.Email = email;

            var hasPassword = await userManager.HasPasswordAsync(user);

            if (hasPassword)
            {
                var removePasswordResult = await userManager.RemovePasswordAsync(user);

                if (!removePasswordResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join("; ", removePasswordResult.Errors.Select(e => e.Description)));
                }
            }

            var addPasswordResult = await userManager.AddPasswordAsync(user, password);

            if (!addPasswordResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", addPasswordResult.Errors.Select(e => e.Description)));
            }

            await userManager.UpdateAsync(user);

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}