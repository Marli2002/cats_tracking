using CATSTracking.Library.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace CATSTracking.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SetupController : ControllerBase
    {

        private readonly RoleManager<IdentityRole> _roleEngine;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly CATSContext _context;
        private List<string> listOfInstallActions = new List<string>();

        public SetupController(RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager, CATSContext context)
        {
            _roleEngine = roleManager;
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("install")]
        [AllowAnonymous]
        public async Task<IActionResult> install()
        {

            Console.WriteLine("Starting setup process...");

            if (!await CanSetupRun())
            {
                return BadRequest(JsonSerializer.Serialize(listOfInstallActions));
            }

            try
            {
                await InstallDefaultRoles();
                await InstallDefaultAdmin();
                return Ok(JsonSerializer.Serialize(listOfInstallActions));
            }
            catch
            {
                return BadRequest(JsonSerializer.Serialize(listOfInstallActions));
            }

        }

        private async Task<bool> CanSetupRun()
        {
            try
            {

                await InitializeDatabaseWithEFCore();
                await EnsureNoExistingUsers();
                return true;

            }
            catch (System.Exception e)
            {
                listOfInstallActions.Add("ERROR: " + e.Message);
                return false;
            }

        }

        private async Task EnsureNoExistingUsers()
        {
            try
            {
                var dbContainsUsers = await _userManager.Users.AnyAsync();
                if (!dbContainsUsers)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to check for existing users: " + e.Message);
            }

            throw new Exception("Setup cannot run because there are existing users in the database.");

        }

        private async Task InitializeDatabaseWithEFCore()
        {

            try
            {
                await _context.Database.MigrateAsync();
                listOfInstallActions.Add("Database initialized.");
                return;
            }
            catch (System.Exception e)
            {
                listOfInstallActions.Add("ERROR: Failed to initialize database");
                throw new Exception("Failed to initialize database:" + e.Message);
            }

        }

        private async Task InstallDefaultRoles()
        {
            string[] defaultRoles = { "Admin", "Employee", "User" };

            foreach (var roleName in defaultRoles)
            {
                if (!await _roleEngine.RoleExistsAsync(roleName))
                {
                    var result = await _roleEngine.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        listOfInstallActions.Add($"Role '{roleName}' created.");
                    }
                    else
                    {
                        listOfInstallActions.Add($"Role '{roleName}' could not be created: {result.Errors.ToList().ToString()}");
                    }
                }
            }
        }

        private async Task InstallDefaultAdmin()
        {
            string adminUsername = "admin";
            string adminPassword = "Password1!";

            var newUser = new IdentityUser
            {
                UserName = adminUsername,
                Email = "admin@admin.com",
                EmailConfirmed = true,
                LockoutEnabled = false
            };

            var result = await _userManager.CreateAsync(newUser, adminPassword);

            if (result.Succeeded)
            {
                listOfInstallActions.Add($"User '{adminUsername}' created with password {adminPassword}.");
                result = await _userManager.AddToRoleAsync(newUser, "Admin");
                if (result.Succeeded)
                {
                    listOfInstallActions.Add($"User '{adminUsername}' added to role 'Administator'.");
                }
                else
                {
                    listOfInstallActions.Add($"Failed to add user '{adminUsername}' to role 'Administator'.");
                }
            }
            else
            {
                listOfInstallActions.Add($"User '{adminUsername}' could not be created: {result.Errors.ToList().ToString()}");

            }
        }


    }
}
