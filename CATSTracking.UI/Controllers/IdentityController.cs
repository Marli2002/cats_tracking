using CATSTracking.Library.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using static System.Formats.Asn1.AsnWriter;

namespace CATSTracking.UI.Controllers
{
    public class IdentityController : Controller
    {

        public IdentityController()
        {

        }

        //This method is used to determine what dashboard a user should be dropped into
        [HttpGet]
        public async Task<IActionResult> PathFinder()
        {
            Console.WriteLine("\n\n\n\n============> User Auth State in PathFinder: " + User.Identity.IsAuthenticated);

          
            TempData.Keep("Toast");

            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else
                {
                    return RedirectToAction("Dashboard", "User");
                }
            }

            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        public async Task<IActionResult> Role()
        {
            throw new NotImplementedException("Role in IdentityController in .UI needs to be rebuilt after being decoupled");

            // try
            // {
            //     if (User.Identity.IsAuthenticated)
            //     {
            //         if (await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(User), "Employee"))
            //         {
            //             var existingRoles = await _roleEngine.Roles.ToListAsync();
            //             return Json(existingRoles);
            //         }
            //         else
            //         {
            //             return BadRequest(new { message = "Unauthorized" });
            //         }
            //     }
            //     else
            //     {
            //         return BadRequest(new { message = "Unauthorized" });
            //     }
            // }
            // catch (Exception e)
            // {
            //     return BadRequest(new { message = "Unauthorized" });
            // }
        }

        [HttpGet]
        public async Task<IActionResult> UserList()
        {
            throw new NotImplementedException("UserList in IdentityController in .UI needs to be rebuilt after being decoupled");

            //  try
            // {
            //     var existingUsers = await _userManager.Users.ToListAsync();

            //     if (User.Identity.IsAuthenticated)
            //     {
            //         if (await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(User), "Employee"))
            //         {
            //             return Json(existingUsers);
            //         }
            //         else
            //         {
            //             return BadRequest(new { message = "Unauthorized" });
            //         }
            //     }
            //     else
            //     {
            //         return BadRequest(new { message = "Unauthorized" });
            //     }
            // }
            // catch (Exception e)
            // {

            //     return BadRequest(new { message = "Unauthorized" });
            // }

        }

        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] BasicUser currUser)
        {
            throw new NotImplementedException("AddUser in IdentityController in .UI needs to be rebuilt after being decoupled");
    //        if (ModelState.IsValid)
    //        {

    //            var newUser = new IdentityUser
    //            {
    //                UserName = currUser.Email,
    //                Email = currUser.Email,
    //                EmailConfirmed = true,
    //                LockoutEnabled = false
    //            };

    //            var result = await _userManager.CreateAsync(newUser, currUser.Password);

    //            if (result.Succeeded)
    //            {
    //                result = await _userManager.AddToRoleAsync(newUser, currUser.RoleName);

    //                if (result.Succeeded)
    //                {
    //                    return Ok(new { result = $"Created user {currUser.Email}" });
    //                }
    //            }

    //            StringBuilder sbuilder = new StringBuilder();

    //            foreach (var error in result.Errors)
    //            {
    //                sbuilder.Append(error.Description);
    //            }

    //            return BadRequest(new { result = $"{sbuilder.ToString()}" });

    //        }

    //        return BadRequest(new { result = "Failed to create new user" });
    }

    }

}
