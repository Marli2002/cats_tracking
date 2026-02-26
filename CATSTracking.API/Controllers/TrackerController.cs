using CATSTracking.Library.Data;
using CATSTracking.Library.Services;
using CATSTracking.Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CATSTracking.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class TrackerController : Controller
    {

        private readonly CATSContext _context;
        private readonly SMSService _smsService;
      

        public List<Tracker> trackerData = new List<Tracker>();

        public TrackerController(CATSContext context, SMSService smsService)
        {
            _context = context;
            _smsService = smsService;
        }


        #region Actions


        [HttpGet("list")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Get a list of all trackers",
        Description = "Retrieves a list of all trackers in the database. Restricted to Admins."
        )]
        public async Task<IActionResult> GetTrackers()
        {
            try
            {
                var trackers = await _context.Trackers.Include(t => t.Login).ToListAsync();
                return Ok(trackers);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }
      
        [HttpGet("userlist")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Get trackers for the current user",
        Description = "Retrieves a list of trackers that are assigned to the currently logged-in user. " +
                   "Users will only see their own trackers based on their assigned Tracker records."
        )]
        [HttpGet("mytrackers")]
        [Authorize] 
        public async Task<IActionResult> GetUserTrackers()
        {
            // Get logged-in user's email
            string userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Cannot determine current user.");

            // Lookup user GUID
            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
                return NotFound("User not found.");

            // Fetch trackers for that user
            var trackers = await _context.Trackers
                .Where(t => t.AddedByLoginId == user.Id)
                .ToListAsync();

            return Ok(trackers);
        }




        [HttpGet("assigned/{userId}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Get all trackers assigned to a specific user (Admin only).",
        Description = "Retrieves all tracker records assigned to the given user ID."
        )]
        public async Task<IActionResult> GetTrackersByUserId(int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid user ID.");

            try
            {
                
                var user = await _context.UserProfiles
                    .SingleOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return NotFound("User not found.");

               
                var trackers = await _context.Trackers
                    .Where(t => t.AddedByLoginId == user.LoginId)
                    .ToListAsync();

                if (!trackers.Any())
                    return NotFound("No trackers found for this user.");

                return Ok(trackers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpPost("new")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddTracker([FromBody] Tracker newTracker)
        {
            if (newTracker == null)
                return BadRequest("Invalid tracker data.");

            try
            {
                if (string.IsNullOrWhiteSpace(newTracker.IMEI) ||
                    string.IsNullOrWhiteSpace(newTracker.SerialNo) ||
                    string.IsNullOrWhiteSpace(newTracker.PhoneNumber) ||
                    string.IsNullOrWhiteSpace(newTracker.DisplayName) ||
                    string.IsNullOrWhiteSpace(newTracker.AddedByLoginId))
                {
                    return BadRequest("Missing required tracker information.");
                }

                newTracker.UTCLastSet = DateTime.UtcNow;
                newTracker.Enabled = true;

                _context.Trackers.Add(newTracker);
                await _context.SaveChangesAsync();

                await LogActivityAsync(
                "Add Tracker",
                $"Tracker '{newTracker.DisplayName}' added.",
                 User.Identity?.Name ?? "Unknown"
                );


                AdoptTrackerAsync(newTracker.PhoneNumber);

                return Ok(new
                {
                    id = newTracker.Id,
                    message = "Tracker successfully added."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("EF SaveChanges failed: " + ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine("Inner exception: " + ex.InnerException.Message);

                return StatusCode(500,
                    $"Internal server error: {ex.InnerException?.Message ?? ex.Message}");
            }
        }


        private async Task AdoptTrackerAsync(string phoneNumber)
        {
            try
            {
                Debug.WriteLine($"Attempting to adopt by phone number {phoneNumber}.");

                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return;
                }

                string[] smsCommands = new[]
                {
                    "Begin123456",
                    "Lag1",
                    "Upload123456 120",
                    "Admin123456 168.119.165.47 10155"
                };

                foreach (var command in smsCommands)
                {
                    bool result = await _smsService.SendSMSAsync(phoneNumber, command);

                    if (!result)
                    {
                        Debug.WriteLine($"Failed to send SMS command: {command}");
                        return;
                    }

                    Debug.WriteLine($"Successfully sent SMS command: {command}");

                    // The trackers take time to process each command, so we wait before sending the next one
                    await Task.Delay(TimeSpan.FromMinutes(2));
                }

                Debug.WriteLine($"Successfully adopted by phone number {phoneNumber}.");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while adopting phonenumber {phoneNumber}: {ex.Message}");
                return;
            }
        }



        [HttpGet("")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Retrieve detailed information about a tracker",
        Description = "Allows the retrieval of detailed information about a tracker by querying one of multiple unique identifiers." +
        "To query a tracker, your must provide query parameters 'key' and 'value' where key is the identifier (ID, SerialNo, IMEI, PhoneNumber) and value is the corresponding value for that identifier."
        )]
        public async Task<IActionResult> GetTracker([FromQuery] string key, [FromQuery] object value = null)
        {

            if (value != null)
            {
                Tracker? trackerToReturn = await GetTrackerByKey(key, value);
                return trackerToReturn != null ? Ok(trackerToReturn) : NotFound("Tracker not found.");
            }
            else
            {
                return BadRequest("Identifier value is required.");
            }

        }


        private async Task<Tracker>? GetTrackerByKey(string key, object value)
        {
            Tracker? trackerToReturn = null;

            switch (key.ToUpper())
            {
                case "ID":
                    trackerToReturn = await _context.Trackers.SingleOrDefaultAsync(trackerEntity => trackerEntity.Id == int.Parse(value.ToString()));
                    break;
                case "IMEI":
                    trackerToReturn = await _context.Trackers.SingleOrDefaultAsync(trackerEntity => trackerEntity.IMEI == value.ToString());
                    break;
                case "SERIALNO":
                    trackerToReturn = await _context.Trackers.SingleOrDefaultAsync(trackerEntity => trackerEntity.SerialNo == value.ToString());
                    break;
                case "PHONENUMBER":
                    trackerToReturn = await _context.Trackers.SingleOrDefaultAsync(trackerEntity => trackerEntity.PhoneNumber == value.ToString());
                    break;
                default:
                    break;
            }

            return trackerToReturn;
        }



        [HttpPatch("{id}/owner")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Set the owner of a tracker.",
        Description = "Sets the owner of a tracker by Id. Once an owner is set, they are able to see the tracker activity. The owner can be set as isAdmin to allow control over the tracker." +
        "NOTE: Only Id works. IMEI, SerialNo etc does not."
        )]
        public async Task<IActionResult> AssignTrackerToOwner(int id, [FromBody] string ownerId, [FromBody] bool isAdmin = false)
        {
            if (id <= 0 || string.IsNullOrEmpty(ownerId))
            {
                return BadRequest("You must provide a tracker ID and Owner ID.");
            }

            try
            {
                var tracker = await _context.Trackers.SingleOrDefaultAsync(trackerEntity => trackerEntity.Id == id);
                if (tracker == null)
                {
                    return NotFound("Tracker not found.");
                }

                var owner = await _context.Users.SingleOrDefaultAsync(user => user.Id == ownerId);
                if (owner == null)
                {
                    return NotFound("Owner not found.");
                }


                UserTracker? existingRelationship = await _context.UserTrackers
                    .SingleOrDefaultAsync(x => x.LoginId == owner.Id && x.TrackerId == tracker.Id);

                UserTracker? existingTrackerEntry = await _context.UserTrackers
                .SingleOrDefaultAsync(x => x.LoginId == owner.Id && x.TrackerId == tracker.Id);

                if (existingRelationship == null && existingTrackerEntry == null)
                {
                    UserTracker newRelationship = new UserTracker
                    {
                        LoginId = owner.Id,
                        TrackerId = tracker.Id,
                        TrackerAdmin = isAdmin
                    };
                    var result = _context.UserTrackers.Add(newRelationship);
                    if (result != null)
                    {
                        await _context.SaveChangesAsync();
                        return Ok("Tracker assigned to owner successfully.");
                    }
                    else
                    {
                        return StatusCode(500, "An error occurred while assigning the tracker to the owner.");
                    }

                }
                else
                {
                    return Conflict("An owner is already assigned to this tracker.");
                }


            }
            catch (System.Exception)
            {
                return StatusCode(500, "An error occurred while assigning the tracker to the owner.");
            }
        }

        [HttpGet("{Id}/owner")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Get the owner of a tracker.",
        Description = "Get the current assigned owner information for a tracker by Id. NOTE: Only Id works. IMEI, SerialNo etc does not."
        )]
        public async Task<IActionResult> GetTrackerOwner(string Id)
        {
            var tracker = await _context.Trackers.SingleOrDefaultAsync(trackerEntity => trackerEntity.IMEI == Id);
            if (tracker == null)
            {
                return NotFound("Tracker not found.");
            }
            var owner = await _context.Users.SingleOrDefaultAsync(x => x.Id == tracker.AddedByLoginId);
            if (owner == null)
            {
                return NotFound("Owner not found.");
            }
            return Ok(owner);
        }

       

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Delete a tracker by ID.",
        Description = "Deletes a tracker and all associated data. Restricted to Admin users.")]
        public async Task<IActionResult> DeleteTracker([FromRoute] int id)
        {
            try
            {
                var tracker = await _context.Trackers
                    .Include(t => t.Login)
                    .SingleOrDefaultAsync(t => t.Id == id);

                if (tracker == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tracker not found"
                    });
                }

                
                var userTrackerRelationships = await _context.UserTrackers
                    .Where(ut => ut.TrackerId == tracker.Id)
                    .ToListAsync();

                if (userTrackerRelationships.Any())
                {
                    _context.UserTrackers.RemoveRange(userTrackerRelationships);
                }

                
                var trackerActivities = await _context.TrackerActivities
                    .Where(ta => ta.TrackerId == tracker.Id)
                    .ToListAsync();

                if (trackerActivities.Any())
                {
                    _context.TrackerActivities.RemoveRange(trackerActivities);
                }

              
                _context.Trackers.Remove(tracker);

               
                await _context.SaveChangesAsync();

                await LogActivityAsync(
                "Delete Tracker",
                $"Tracker '{tracker.DisplayName}' deleted.",
                User.Identity?.Name ?? "Unknown"
                );

                return Ok(new
                {
                    success = true,
                    message = "Tracker deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting the tracker",
                    error = ex.Message
                });
            }
        }

        // Endpont that gets the location data for trackers between the from and to date
        [HttpGet("{id}/locationdata")]
        public async Task<IActionResult> GetTrackerLocationData(string id, [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            // TODO : Implement logic to get the location data for the tracker between the specified dates
            var tracker = await _context.Trackers.SingleOrDefaultAsync(trackerEntity => trackerEntity.IMEI == id);
            if (tracker == null)
            {
                return NotFound("Tracker not found.");
            }
            else
            {
                var locationData = await _context.TrackerActivities
                    .Where(activity => activity.TrackerId == tracker.Id && activity.ActivityType == "Location" && activity.UTCDateTime >= from && activity.UTCDateTime <= to)
                    .ToListAsync();
                return Ok(locationData);
            }
        }

        // Endpoint to ingest tracker data from a specific format
        [HttpPost("checkin")]

        public async Task<IActionResult> IngestTrackerData([FromQuery] string? tokenId)
        {
            if (string.Equals(tokenId, "HardcodedToken6523"))
            {
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    var line = await reader.ReadToEndAsync();
                    var match = Regex.Match(line, @"\*.*?#");
                    if (match.Success)
                    {

                        // We read the data based on this doc: https://www.traccar.org/protocol/5013-h02/GPS+Tracker+Platform+Communication+Protocol-From+Winnie+HuaSunTeK-V1.0.5-2017.pdf, pg 2

                        //Parse the incoming data by using split
                        string[] trackerData = match.Value.Split(",");

                        string trackerIMEI = trackerData[1];
                        string trackerActionName = trackerData[2];
                        string currLatitude = trackerData[5];
                        string latitudeDir = trackerData[6];   // N or S
                        string currLongitude = trackerData[7];
                        string longitudeDir = trackerData[8];  // E or W

                        // Do calculated string to determine location string for openlayers etc
                        string currLocationString = $"{currLatitude}{latitudeDir}, {currLongitude}{longitudeDir}";

                        Library.Models.Tracker matchingTracker = await _context.Trackers.SingleOrDefaultAsync(x => x.IMEI == trackerIMEI); //essentially, return the first tracker we find with the matching IMEI

                        if (matchingTracker != null)
                        {
                            Library.Models.TrackerActivity newTrackerActivityEntry = new Library.Models.TrackerActivity
                            {
                                UTCDateTime = DateTime.UtcNow,
                                TrackerId = matchingTracker.Id,
                                ActivityType = trackerActionName,
                                Value = currLocationString
                            };
                            _context.TrackerActivities.Add(newTrackerActivityEntry);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            //TODO : Add error event logging
                            return NotFound("404 not found");
                        }
                        return Ok("Data ingested and sent to Pushover.");
                    }
                    else
                    {
                        Console.WriteLine("Ignoring trash data");
                        return BadRequest("Invalid data format.");
                    }
                }
            }
            else
            {
                return Unauthorized("404 not found");
            }
        }


        [HttpGet("{id}/openstreetmapactivity")]
        public async Task<IActionResult> GetOpenStreetMapActivity(int id)
        {
            // Get the tracker by ID
            var tracker = await _context.Trackers.SingleOrDefaultAsync(x => x.Id == id);
            if (tracker == null)
                return NotFound("Tracker not found.");

            // Get all location activities for this tracker
            var activities = await _context.TrackerActivities
                .Where(x => x.TrackerId == tracker.Id && x.ActivityType != null)
                .OrderBy(x => x.UTCDateTime)
                .ToListAsync();

            // Map each activity to decimal coordinates
            var result = activities.Select(activity =>
            {
                // Ensure Value is not null or empty
                if (string.IsNullOrWhiteSpace(activity.Value))
                    return null;

                // Split the raw value in db into lat and lon parts
                var parts = activity.Value.Split(",");
                if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                    return null;

                string latPart = parts[0].Trim();
                string lonPart = parts[1].Trim();

                // Ensure parts are long enough to extract direction
                if (latPart.Length < 1 || lonPart.Length < 1)
                    return null;

                string rawLat = latPart.Substring(0, latPart.Length - 1);
                string latDir = latPart.Substring(latPart.Length - 1, 1);
                string rawLon = lonPart.Substring(0, lonPart.Length - 1);
                string lonDir = lonPart.Substring(lonPart.Length - 1, 1);

                // Ensure raw values are valid
                if (string.IsNullOrWhiteSpace(rawLat) || string.IsNullOrWhiteSpace(rawLon))
                    return null;

                double decimalLat = TryConvertDdmToDecimal(rawLat, latDir, true);
                double decimalLon = TryConvertDdmToDecimal(rawLon, lonDir, false);

                return new
                {
                    imei = tracker.IMEI,
                    rawLatitude = rawLat + latDir,
                    rawLongitude = rawLon + lonDir,
                    decimalLatitude = decimalLat,
                    decimalLongitude = decimalLon,
                    osM_Link = $"https://www.openstreetmap.org/?mlat={decimalLat.ToString(CultureInfo.InvariantCulture)}&mlon={decimalLon.ToString(CultureInfo.InvariantCulture)}&zoom=15",
                    timestamp = activity.UTCDateTime.ToString("u")
                };
            }).Where(x => x != null).ToList();

            return Ok(result);
        }

        // link to the chat that helped create this method https://chatgpt.com/share/68f34ab6-1a5c-8001-bd65-f346346c1afc
        private static double TryConvertDdmToDecimal(string ddmValue, string direction, bool isLatitude)
        {
            if (string.IsNullOrWhiteSpace(ddmValue))
                return 0;

            ddmValue = ddmValue.Trim();
            if (ddmValue.Length < 4)
                return 0;

            int degreeLength = isLatitude ? 2 : 3;
            if (ddmValue.Length <= degreeLength)
                return 0;

            string degPart = ddmValue.Substring(0, degreeLength);
            string minPart = ddmValue.Substring(degreeLength);

            if (!double.TryParse(degPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double degrees))
                return 0;

            if (!double.TryParse(minPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double minutes))
                return 0;

            double decimalDegrees = degrees + (minutes / 60.0);

            if (!string.IsNullOrWhiteSpace(direction) &&
                (direction.Equals("S", StringComparison.OrdinalIgnoreCase) || direction.Equals("W", StringComparison.OrdinalIgnoreCase)))
            {
                decimalDegrees *= -1.0;
            }

            if (isLatitude && (decimalDegrees < -90 || decimalDegrees > 90))
                return 0;
            if (!isLatitude && (decimalDegrees < -180 || decimalDegrees > 180))
                return 0;

            return Math.Round(decimalDegrees, 7);
        }

        [HttpGet("all/openstreetmapactivity")]
        [Authorize]
        [SwaggerOperation(
            Summary = "This gets the tracker activity for all trackers, for admins.",
            Description = "Admins can see all the tracker activity at once"
        )]
        public async Task<IActionResult> GetAllOpenStreetMapActivity([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var trackers = await _context.Trackers.ToListAsync();
            var result = new List<object>();

            foreach (var tracker in trackers)
            {
                var activities = await _context.TrackerActivities
                    .Where(x => x.TrackerId == tracker.Id && x.ActivityType != null)
                    .Where(x => !from.HasValue || x.UTCDateTime >= from)
                    .Where(x => !to.HasValue || x.UTCDateTime <= to)
                    .OrderBy(x => x.UTCDateTime)
                    .ToListAsync();

                var trackerLocations = activities.Select(activity =>
                {
                    // Ensure Value is not null or empty
                    if (string.IsNullOrWhiteSpace(activity.Value))
                    {
                        Console.WriteLine($"Invalid Value format for TrackerActivity ID {activity.Id}: {activity.Value}");
                        return null;
                    }

                    // Split the raw value in db into lat and lon parts
                    var parts = activity.Value.Split(",");
                    if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                    {
                        Console.WriteLine($"Invalid Value format for TrackerActivity ID {activity.Id}: {activity.Value}");
                        return null;
                    }

                    string latPart = parts[0].Trim();
                    string lonPart = parts[1].Trim();

                    // Ensure parts are long enough to extract direction
                    if (latPart.Length < 1 || lonPart.Length < 1)
                    {
                        Console.WriteLine($"Invalid Value format for TrackerActivity ID {activity.Id}: {activity.Value}");
                        return null;
                    }

                    string rawLat = latPart.Substring(0, latPart.Length - 1);
                    string latDir = latPart.Substring(latPart.Length - 1, 1);
                    string rawLon = lonPart.Substring(0, lonPart.Length - 1);
                    string lonDir = lonPart.Substring(lonPart.Length - 1, 1);

                    // Ensure raw values are valid
                    if (string.IsNullOrWhiteSpace(rawLat) || string.IsNullOrWhiteSpace(rawLon))
                    {
                        Console.WriteLine($"Invalid Value format for TrackerActivity ID {activity.Id}: {activity.Value}");
                        return null;
                    }

                    double decimalLat = TryConvertDdmToDecimal(rawLat, latDir, true);
                    double decimalLon = TryConvertDdmToDecimal(rawLon, lonDir, false);

                    return new
                    {
                        imei = tracker.IMEI,
                        rawLatitude = rawLat + latDir,
                        rawLongitude = rawLon + lonDir,
                        decimalLatitude = decimalLat,
                        decimalLongitude = decimalLon,
                        osM_Link = $"https://www.openstreetmap.org/?mlat={decimalLat.ToString(CultureInfo.InvariantCulture)}&mlon={decimalLon.ToString(CultureInfo.InvariantCulture)}&zoom=15",
                        timestamp = activity.UTCDateTime.ToString("u")
                    };
                }).Where(x => x != null).ToList();

                result.AddRange(trackerLocations);
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Get a tracker by ID",
            Description = "Returns tracker details for a specific tracker ID. Admin only."
        )]
        public async Task<IActionResult> GetTrackerById(int id)
        {
            try
            {
                var tracker = await _context.Trackers
                    .Include(t => t.Login)
                    .SingleOrDefaultAsync(t => t.Id == id);

                if (tracker == null)
                    return NotFound("Tracker not found.");

                return Ok(tracker);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the tracker: {ex.Message}");
            }
        }


        [HttpPut("{id}/edit")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Edit tracker details and reprogram if phone number changed.",
            Description = "Allows admins to edit tracker info. If the phone number changes, the tracker will be reprogrammed automatically."
        )]
        public async Task<IActionResult> EditTracker(int id, [FromBody] Tracker updatedTracker)
        {
            if (updatedTracker == null)
                return BadRequest("Invalid tracker data.");

            try
            {
                var existingTracker = await _context.Trackers.SingleOrDefaultAsync(t => t.Id == id);
                if (existingTracker == null)
                    return NotFound("Tracker not found.");

           
                bool phoneNumberChanged = !string.Equals(existingTracker.PhoneNumber, updatedTracker.PhoneNumber, StringComparison.OrdinalIgnoreCase);

              
                existingTracker.DisplayName = updatedTracker.DisplayName ?? existingTracker.DisplayName;
                existingTracker.SerialNo = updatedTracker.SerialNo ?? existingTracker.SerialNo;
                existingTracker.IMEI = updatedTracker.IMEI ?? existingTracker.IMEI;
                existingTracker.PhoneNumber = updatedTracker.PhoneNumber ?? existingTracker.PhoneNumber;
                existingTracker.Enabled = updatedTracker.Enabled;
                existingTracker.UTCLastSet = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await LogActivityAsync(
                "Edit Tracker",
                 $"Tracker '{existingTracker.DisplayName}' updated.",
                 User.Identity?.Name ?? "Unknown"
                );


                if (phoneNumberChanged)
                {
                    _ = Task.Run(async () =>
                    {
                        await AdoptTrackerAsync(updatedTracker.PhoneNumber);
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = phoneNumberChanged
                        ? "Tracker updated and reprogramming initiated."
                        : "Tracker updated successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating the tracker.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("dashboard")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Get dashboard summary for the current user",
        Description = "Provides summary information for the dashboard for the currently logged-in user, including total trackers, active trackers, and recent activities."
        )]
        public async Task<IActionResult> GetUserDashboard()
        {
            try
            {
                string userEmail = User.Identity?.Name;
                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized("Cannot determine current user.");

                var user = await _context.Users
                    .SingleOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return NotFound("User not found.");


                var userTrackers = await _context.UserTrackers
                    .Where(ut => ut.LoginId == user.Id)
                    .Select(ut => ut.TrackerObj)
                    .ToListAsync();

                int totalTrackers = userTrackers.Count;
                int activeTrackers = userTrackers.Count(t => t.Enabled);

                DateTime? lastUpdate = userTrackers
                    .OrderByDescending(t => t.UTCLastSet)
                    .Select(t => t.UTCLastSet)
                    .FirstOrDefault();


                var recentActivities = await _context.TrackerActivities
                    .Where(a => userTrackers.Select(t => t.Id).Contains(a.TrackerId))
                    .OrderByDescending(a => a.UTCDateTime)
                    .Take(10)
                    .Select(a => new
                    {
                        TrackerId = a.TrackerId,
                        ActivityType = a.ActivityType,
                        Value = a.Value,
                        Timestamp = a.UTCDateTime
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalTrackers,
                    activeTrackers,
                    lastUpdate,
                    recentActivities
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while fetching dashboard data",
                    error = ex.Message
                });
            }
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


        #endregion

        #region Data Management

        #endregion
    }
}
