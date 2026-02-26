using CATSTracking.Library.Data;
using CATSTracking.Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CATSTracking.Library.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Net;

namespace CATSTracking.API.Controllers
{

    [Route("api/v1/[controller]")]
    public class UserController : Controller
    {

        private readonly RoleManager<IdentityRole> _roleEngine;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<UserController> _logger;
        private readonly CATSContext _context;

        public UserController(RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager, CATSContext context, ILogger<UserController> logger)
        {
            _roleEngine = roleManager;
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        #region User Profile Management

        [HttpGet("{id}")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Get profile for a user by ID. Support /me shortcut.",
        Description = "Returns the profile information for a user by their ID."
        + "This does NOT return the users password. The /me shortcut is supported to get the information of the logged in user.")]
        public async Task<IActionResult> GetUser([FromQuery] int id)
        {

            try
            {
                UserProfile? userToReturn = _context.UserProfiles.SingleOrDefaultAsync<UserProfile>(u => u.Id == id).Result;

                return userToReturn != null ? Ok(userToReturn) : NotFound();
            }
            catch (System.Exception)
            {
                return BadRequest();
            }

        }

        [HttpPost("new")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Create a new user profile and login.",
        Description = "Creates a new user profile and login. This endpoint is restricted to Admin users only.")]
        public async Task<IActionResult> NewUser([FromBody] UserProfile user)
        {
            try
            {

                var newUserLogin = new IdentityUser
                {
                    UserName = user.LoginId,
                    Email = user.LoginId,
                    EmailConfirmed = false,
                    LockoutEnabled = false
                };

                var loginCreateResult = await _userManager.CreateAsync(newUserLogin, "Default@123");

                if (!loginCreateResult.Succeeded)
                {
                    return BadRequest(loginCreateResult.Errors);
                }

                // Added this line to login as a User. We can remove this but i can only login if this line is here.
                await _userManager.AddToRoleAsync(newUserLogin, "User");


                UserProfile newUser = new UserProfile
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    APIUser = false,
                    Login = newUserLogin
                };

                var createdUser = await _context.AddAsync<UserProfile>(newUser);
                await _context.SaveChangesAsync();

                await LogActivityAsync(
                "Add User",
                $"New user '{newUser.LoginId}' created.",
                User.Identity?.Name ?? "Unknown"
                );

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(newUserLogin);
                var encodedToken = WebUtility.UrlEncode(resetToken);

                return createdUser != null ? Ok(new
                {
                    id = newUserLogin.Id,
                    resetToken = encodedToken,
                    rawToken = resetToken
                }) : BadRequest();
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"\n\n\n\nException creating new user: {ex.Message}\n\n\n");
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("password")]
        [SwaggerOperation(
        Summary = "Reset a user's password.",
        Description = "Resets the password for a user. Requires a valid password reset token.")]
        public async Task<IActionResult> Password([FromBody] CATSTracking.Library.Models.Login updatedLogin)
        {
            try
            {
                var decodedToken = WebUtility.UrlDecode(updatedLogin.ResetToken);
                System.Console.WriteLine($"\n\n\n\nUpdating password for: {updatedLogin.Username}\nTo: {updatedLogin.Password}\nToken: {decodedToken}\n\n\n");
                IdentityUser userLoginToUpdate = await _userManager.FindByIdAsync(updatedLogin.Username);

                if (userLoginToUpdate == null)
                    return NotFound("User not found.");

                var result = await _userManager.ResetPasswordAsync(userLoginToUpdate, decodedToken, updatedLogin.Password);

                System.Console.WriteLine($"\n\n\n\nPassword reset result: {JsonConvert.SerializeObject(result)}\n\n\n");
                if (!result.Succeeded)
                {
                    System.Console.WriteLine($"\n\n\n\nPassword reset failed: {JsonConvert.SerializeObject(result.Errors)}\n\n\n");
                    return BadRequest("Invalid token or password, reset failed.");
                }

                return Ok(new
                {
                    id = userLoginToUpdate.Id,
                    Message = "Password successfully updated."
                });

            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        [HttpGet("")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Get a list of all users",
        Description = "Retrieves a list of all user profiles in the database. This endpoint is restricted to Admin users only."
        )]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.UserProfiles.Include(u => u.Login).ToListAsync();
                return Ok(users);
            }
            catch (System.Exception)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }



        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Update the user by ID. Supports the /me shortcut.",
        Description = "Update the profile and/or login of a user by their ID. This endpoint is restricted to Admin users only." +
        "The /me shortcut is supported to update the information of the logged in user." +
        "Important: Leave the password empty or null to avoid updating it")]
        public async Task<IActionResult> UpdateUser([FromQuery] int id, [FromBody] BasicUser user)
        {
            try
            {
                UserProfile? userToUpdate = _context.UserProfiles.Include(u => u.Login).SingleOrDefaultAsync<UserProfile>(u => u.Id == id).Result;

                if (userToUpdate == null)
                {
                    return NotFound();
                }

                userToUpdate.FirstName = user.FirstName;
                userToUpdate.LastName = user.LastName;
                userToUpdate.Login.Email = user.Email;
                userToUpdate.Login.UserName = user.Email;

                if (!string.IsNullOrEmpty(user.Password))
                {
                    userToUpdate.Login.PasswordHash =
                        new Microsoft.AspNetCore.Identity.PasswordHasher<Microsoft.AspNetCore.Identity.IdentityUser>()
                        .HashPassword(userToUpdate.Login, user.Password);
                }

                var updatedUser = _context.Update<UserProfile>(userToUpdate);
                await _context.SaveChangesAsync();

                return updatedUser != null ? Ok(updatedUser.Entity) : BadRequest();
            }
            catch (System.Exception)
            {
                return BadRequest();
            }
        }

        #endregion


        [HttpGet("{id}/activity")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Get profile activity. Supports /me shortcut.",
        Description = "Get the activity logs for a given user by ID. Supports the /me shortcut.")]
        public async Task<IActionResult> GetUserActivity([FromQuery] int id)
        {

            try
            {
                UserProfile? userToMonitor = _context.UserProfiles.SingleOrDefaultAsync<UserProfile>(u => u.Id == id).Result;

                if (userToMonitor == null)
                {
                    return NotFound();
                }

                List<EventLog> userActivities = _context.EventLogs.Where(e => e.LoginId == userToMonitor.LoginId).ToListAsync().Result;
                return Ok(userActivities);
            }
            catch (System.Exception)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Delete a user by ID.",
        Description = "Deletes a user profile and their associated login. Restricted to Admin users.")]
        public async Task<IActionResult> DeleteUser([FromRoute] int id)
        {
            try
            {

                var user = await _context.UserProfiles
                    .Include(u => u.Login)
                    .SingleOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    _logger.LogWarning("Delete attempt failed: User {UserId} not found.", id);
                    return NotFound(new
                    {
                        success = false,
                        message = "User not found"
                    });
                }


                if (user.Login != null)
                {
                    // Delete the Identity user
                    var identityDelete = await _userManager.DeleteAsync(user.Login);
                    if (!identityDelete.Succeeded)
                    {
                        // Log the errors
                        _logger.LogError("Failed to delete Identity user {UserId}. Errors: {Errors}",
                            id, string.Join(", ", identityDelete.Errors.Select(e => e.Description)));
                        return BadRequest(new
                        {

                            success = false,
                            message = "Failed to delete user login",
                            errors = identityDelete.Errors.Select(e => e.Description)
                        });
                    }
                    else
                    {
                        _logger.LogInformation("Identity user {UserId} deleted successfully.", id);
                    }
                }

                // deletes the UserProfile
                _context.UserProfiles.Remove(user);
                // Save changes to the database
                await _context.SaveChangesAsync();

                await LogActivityAsync(
                "Delete User",
                $"User '{user.LoginId}' deleted.",
                User.Identity?.Name ?? "Unknown"
                );

                _logger.LogInformation("User profile {UserId} deleted successfully from database.", id);

                return Ok(new
                {
                    success = true,
                    message = "User deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting the user",
                    error = ex.Message
                });
            }
        }

        [HttpGet("recent")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRecentActivity(int count = 10)
        {
            var activities = await _context.ActivityLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .Select(a => new ActivityLog
                {
                    Action = a.Action,
                    Details = a.Details,
                    PerformedBy = a.PerformedBy,
                    Timestamp = a.Timestamp
                })
                .ToListAsync();

            return Ok(activities);
        }



        public async Task LogActivityAsync(string action, string details, string performedBy)
        {
            try
            {
                var log = new ActivityLog
                {
                    Action = action,
                    Details = details,
                    PerformedBy = performedBy,
                    Timestamp = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
               
            }
        }


    }
}


