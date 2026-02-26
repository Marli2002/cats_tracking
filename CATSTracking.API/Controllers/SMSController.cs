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
public class SMSController : ControllerBase
{
    private readonly CATSContext _context;
    private readonly SMSService _smsService;
    private readonly EventLogService _eventLogService;


    public SMSController(CATSContext context, SMSService smsService, EventLogService eventLogService)
    {
        _context = context;
        _smsService = smsService;
        _eventLogService = eventLogService;
    }

    [HttpPost("sendtest")]
    [AllowAnonymous]
    public async Task<IActionResult> SendTestSMS([FromQuery] string token, [FromQuery] string body, [FromQuery] string to)
    {
        if (token != "5d1a9358-781f-4ccd-bd24-533ddab801a5")
        {
            return Unauthorized();
        }

        try
        {
            var result = await _smsService.SendSMSAsync(to, $"{DateTime.UtcNow}UTC:\n{body}");

            if (!result)
            {
                return StatusCode(500, "Failed to send the SMS.");
            }

            await _eventLogService.LogEventAsync("SMS",$"Test SMS sent successfully to {to}.");
            return Ok("SMS sent successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    // TODO: Need to store this token somewhere and retrieve it.
    // for now it is just in testing
    [HttpPost("receive")]
    [AllowAnonymous]
    [SwaggerOperation(
    Summary = "Receive an SMS from Twilio webhook. A bearer token in the 'token' parameter is required.",
    Description = "Ingests incoming messages from the twillio api"
    )]
    public async Task<IActionResult> receive(SmsRequest incomingMessage, [FromQuery] string token)
    {
        if (token != "5d1a9358-781f-4ccd-bd24-533ddab801a5")
        {
            return Unauthorized();
        }

        try
        {
            if (!string.IsNullOrEmpty(incomingMessage.Body))
            {

                var messagingResponse = new MessagingResponse();

                var smsData = new CATSTracking.Library.Models.SMS
                {
                    From = incomingMessage.From,
                    To = incomingMessage.To,
                    Message = incomingMessage.Body
                };

                var newSMS = await _context.AddAsync<CATSTracking.Library.Models.SMS>(smsData);

                if (newSMS == null)
                {
                    throw new Exception("Failed to add SMS to the database.");
                }

                await _context.SaveChangesAsync();
            }
        }
        catch (System.Exception)
        {
            return StatusCode(500, "An error occurred while saving the SMS message.");
        }

        return Ok();
    }

}