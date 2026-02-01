using Microsoft.AspNetCore.Identity;

namespace worldRecipeMvc.Data
{
    public enum Roles
    {
        User,
        Moderator,
        Admin

    }
    public static class ContextSeed
    {
        public static async Task SeedRolesAsync(UserManager<RecipeWorldUser> userManager, RoleManager<IdentityRole> roleManager)
        {      
            await roleManager.CreateAsync(new IdentityRole(Roles.User.ToString()));
            await roleManager.CreateAsync(new IdentityRole(Roles.Moderator.ToString()));
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
            

            return;
        }
    }
}
