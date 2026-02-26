using CATSTracking.Library.Models;
using CATSTracking.Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Swashbuckle.AspNetCore.Annotations;

namespace CATSTracking.API.Controllers
{

    [Route("api/v1/[controller]")]
    [ApiController]
    public class tokenController : ControllerBase
    {

        private readonly RoleManager<IdentityRole> _roleEngine;
        private readonly UserManager<IdentityUser> _userEngine;
        private readonly SignInManager<IdentityUser> _authEngine;

        public tokenController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _roleEngine = roleManager;
            _userEngine = userManager;
            _authEngine = signInManager;
        }


        [HttpPost("new")]
        [AllowAnonymous]
        [SwaggerOperation(
        Summary = "Generates a new JWT token using basic authentication",
        Description = "Hit this endpoint with a username and password to authenticate and retrieve a JWT token for the user"
        )]
        public async Task<IActionResult> New([FromBody] TokenRequest requestData)
        {
            try
            {
                System.Console.WriteLine($"\n\n\n\n\nProcessing token request for user: {requestData.Username}");
                var targetUser = await _userEngine.FindByNameAsync(requestData.Username);

                if (targetUser != null)
                {
                    var authResult = await _authEngine.PasswordSignInAsync(requestData.Username, requestData.Password,
                        false, false);
                    if (authResult.Succeeded)
                    {
                        var JWT = TokenService.NewToken(targetUser, _userEngine);
                        return Ok(JWT);
                    }
                }

                return Unauthorized(new RequestError { Code = 404, Description = "The user cannot be found. No token can be generated" });

            }
            catch (SqlException sqlEx) when (sqlEx.Number == 18456)
            {
                Console.WriteLine("CRITICAL: Failed to log into the database. Common causes include:\n\n" +
                "\t Password is incorrect in the connection string.\n" +
                "\t The use of Encrypt=True in the connection string\n" +
                "\t Being unable to reach the database server. Try IP vs. DNS etc.\n\n" +
                "The message from the SQL Client: " + sqlEx.Message);
                return BadRequest(new RequestError { Code = 500, Description = "A critical failure occured while accessing the database." });
            }
            catch (Exception userEngineEx)
            {
                Console.WriteLine("\t\tCRITICAL: " + userEngineEx.ToString());
                return BadRequest(new RequestError { Code = 500, Description = "A critical failure occured while accessing the user engine." });
            }

        }


        [HttpGet("validate")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Check if a JWT is valid",
        Description = "This endpoint performs no action and provides the ability to check if a JWT is still valid. Use this endpoint periodically to ensure a new token is not needed."
        )]
        public async Task<IActionResult> Validate()
        {

            return Ok(new { Valid = true });

        }

        [HttpGet("whoami")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Get user information for the logged in user",
        Description = "Returns information about the user associated with the provided JWT"
        )]
        public async Task<IActionResult> whoami()
        {
            return Ok(new { Username = User.Identity.Name });

        }


    }
}
