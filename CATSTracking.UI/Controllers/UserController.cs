using CATSTracking.Library.Models;
using CATSTracking.Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace CATSTracking.UI.Controllers
{
    public class UserController : Controller
    {

        private readonly ApiService _apiService;

        public UserController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Dashboard(int? trackerId = null)
        {
            try
            {
               
                if (User.Identity?.IsAuthenticated != true || !User.IsInRole("User"))
                {
                    return RedirectToAction("PathFinder", "Identity");
                }

                string userId = User.Identity?.Name;
                string jwtToken = User.FindFirst("jwt")?.Value;

              
                var userTrackers = await _apiService.GetUserTrackersAsync();

                if (!userTrackers.Any())
                {
                    ViewBag.Error = "You have no trackers assigned.";
                    return View(new List<Tracker>());
                }

            
                Tracker selectedTracker = null;
                if (trackerId.HasValue)
                {
                    selectedTracker = userTrackers.FirstOrDefault(t => t.Id == trackerId.Value);
                }
                else
                {
                    selectedTracker = userTrackers.OrderByDescending(t => t.UTCLastSet).FirstOrDefault();
                    trackerId = selectedTracker?.Id;
                }

                ViewBag.Trackers = userTrackers;
                ViewBag.SelectedTrackerId = trackerId;

                List<Location> locations = new List<Location>();
                if (selectedTracker != null)
                {
                    var trackerLocations = await _apiService.GetTrackerLocationsAsync(selectedTracker.Id, jwtToken);
                    
                    locations = trackerLocations.Where(l => l.IMEI == selectedTracker.IMEI).ToList();
                }

                ViewBag.Activities = locations;

             
                ViewBag.Username = userId;
                ViewBag.TotalDevices = userTrackers.Count;
                ViewBag.ActiveDevices = userTrackers.Count(t => t.Enabled);
                ViewBag.RecentActivityCount = locations.Count;
                ViewBag.LastUpdated = selectedTracker?.UTCLastSet.ToString("g");

                return View(userTrackers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Dashboard: {ex.Message}");
                ViewBag.Error = "Something went wrong while loading your dashboard.";
                return View(new List<Tracker>());
            }
        }

        public async Task<IActionResult> LiveMap(int? trackerId = null)
        {
            try
            {
                if (User.Identity?.IsAuthenticated != true || !User.IsInRole("User"))
                {
                    return RedirectToAction("PathFinder", "Identity");
                }

                var trackers = await _apiService.GetTrackerListAsync();
                string userId = User.Identity?.Name;
                var userTrackers = trackers.Where(t => t.AddedByLoginId == userId).ToList();
                ViewBag.Trackers = userTrackers;
                ViewBag.SelectedTrackerId = trackerId;

                List<Location> locations = new List<Location>();
                string jwtToken = User.FindFirst("jwt")?.Value;

                if (trackerId.HasValue)
                {
                    locations = await _apiService.GetTrackerLocationsAsync(trackerId.Value, jwtToken);
                }
                else
                {
                    foreach (var tracker in userTrackers)
                    {
                        var trackerLocations = await _apiService.GetTrackerLocationsAsync(tracker.Id, jwtToken);
                        locations.AddRange(trackerLocations);
                    }
                }

                return View(locations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching location data: {ex.Message}");
                return View(new List<Location>());
            }
        }

        [Authorize]
        public async Task<IActionResult> TrackerList()
        {
            try
            {
                var trackers = await _apiService.GetUserTrackersAsync();
                return View(trackers);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unable to fetch your trackers: {ex.Message}";
                return View(new List<Tracker>());
            }
        }

        public IActionResult Settings()
        {
            return View();
        }

        public IActionResult EditProfile()
        {
            return View();
        }

        public IActionResult ChangePassword()
        {
            return View();
        }
    }
}
