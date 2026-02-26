using CATSTracking.Library.Models;
using CATSTracking.Library.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NuGet.Common;
using System.Diagnostics;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json.Linq;
using System.Net;


namespace CATSTracking.UI.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApiService _apiService;

        public LoginController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Password([FromQuery] string token, [FromQuery] string id)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(id))
            {
                TempData["Toast"] = "Error#Invalid password reset link.";
                return RedirectToAction("Index", "Home");
            }

            var loginInfo = new Login
            {
                Username = id,
                ResetToken = token
            };

            return View(loginInfo);
        }


        // https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.generatepasswordresettokenasync?view=aspnetcore-9.0
        [HttpPost]
        public async Task<IActionResult> Password(Login newLoginInfo)
        {
            try
            {
                System.Console.WriteLine($"\n\n\n\n\nProcessing password reset for user: {newLoginInfo.Username}");
                var result = await _apiService.ResetUserPassword(newLoginInfo);
                if (string.IsNullOrEmpty(result))
                {
                    TempData["Toast"] = "Error#Password reset failed. Please try again.";
                }

                else
                {
                    TempData["Toast"] = "Success#Password reset successfully.";
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Exception during password reset:\n{ex.Message}");
                TempData["Toast"] = "Error#Password reset failed.";

            }

            return View(newLoginInfo);
        }


        [HttpPost]
        public async Task<IActionResult> Index(Login currLogin)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Debug.WriteLine($"Attempting login for user: {currLogin.Username}");

                    var jwt = await _apiService.GetToken(new TokenRequest
                    {
                        Username = currLogin.Username,
                        Password = currLogin.Password,
                        GrantsRequested = "read write"
                    });

                    if (string.IsNullOrEmpty(jwt))
                    {
                        throw new Exception("Unable to authenticate to API.");
                    }

                    // Response.Cookies.Append("JWT", jwt, new CookieOptions
                    // {
                    //     HttpOnly = true,
                    //     SameSite = SameSiteMode.Strict,
                    //     Expires = DateTime.UtcNow.AddHours(1)
                    // });

                    // https://positiwise.com/blog/jwt-authentication-in-net-core
                    // https://developer.okta.com/blog/2019/06/26/decode-jwt-in-csharp-for-authorization
                    // https://www.c-sharpcorner.com/article/jwt-json-web-token-authentication-in-asp-net-core/

                    /*
                        Still a work in progress:
                        This section allows us to use a JWT from the API but encode
                        it to an auth cookie on the UI side. This gives a blend of
                        cookie and JWT Bearer auth that makes it easy for the UI to utilize
                        auth features like [Authorize] and User.IsInRole() while still
                        using JWTs for API auth.
                        The downside is that we have to manually sync the JWT and the cookie.
                        The upside is that we can use the best features of both auth methods.
                    */

                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.ReadJwtToken(jwt);
                    var identity = new ClaimsIdentity(token.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                    Console.WriteLine($"User {currLogin.Username} logged in successfully.");

                    return RedirectToAction("PathFinder", "Identity");

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception during login:\n{e.Message}");
                    TempData["Toast"] = "Error#Login information invalid.";
                }
            }
            return View(currLogin);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            TempData.Remove("Toast"); // clear all Tempdata messages
            //TODO: Invalidate the token on the API side if possible
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

    }
}
