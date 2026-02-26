using System.Diagnostics;
using CATSTracking.Library.Data;
using CATSTracking.Library.Models;
using CATSTracking.Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;


[Route("api/v1/[controller]")]
[ApiController]
public class UtilityController : ControllerBase
{
    [HttpGet("healthcheck")]
    [AllowAnonymous]
    public async Task<IActionResult> healthcheck()
    {
        Console.WriteLine("Healthcheck endpoint was called.");
        return Ok(new { status = "API is healthy", timestamp = DateTime.UtcNow });
    }
}