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

namespace CATSTracking.API.Controllers
{

    [Route("api/v1/[controller]")]
    public class RoleController : Controller
    {

        private readonly CATSContext _context;

        public RoleController(CATSContext context)
        {
            _context = context;
        }


        [HttpGet("")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Get all assignable roles.",
        Description = "Returns a list of all assignable roles.")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _context.Roles.ToListAsync();
                return Ok(roles);
            }
            catch (System.Exception)
            {
                return StatusCode(500, "An error occurred while retrieving roles.");
            }
        }
        

    }
}