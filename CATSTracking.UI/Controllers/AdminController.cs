using CATSTracking.Library.Models;
using Microsoft.AspNetCore.Mvc;
using CATSTracking.Library.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CATSTracking.UI.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApiService _apiService;

        public AdminController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UsersList()
        {
            try
            {
                List<UserProfile> users = await _apiService.GetUserList();
                System.Console.WriteLine($"\n\n\n\n\nFetched {users.Count} users from API.");
                return View(users);
            }
            catch (Exception ex)
            {
                TempData["Toast"] = $"Error#Failed to load users: {ex.Message}";
                System.Console.WriteLine($"\n\n\n\n\nError fetching users: {ex.Message}");
                return View(new List<UserProfile>());
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                await _apiService.DeleteUserAsync(id);
                TempData["Toast"] = "User deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Toast"] = $"Error deleting user: {ex.Message}";
            }
            return RedirectToAction("UsersList");
        }

        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> Index()
        //{
        //    try
        //    {
        //        if (User.Identity?.IsAuthenticated == true)
        //        {
        //            if (!User.IsInRole("Admin"))
        //            {
        //                return RedirectToAction("PathFinder", "Identity");
        //            }
        //            return View();
        //        }
        //        else
        //        {
        //            return RedirectToAction("PathFinder", "Identity");
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return RedirectToAction("PathFinder", "Identity");
        //    }
        //}

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Admin"))
                {
                    return RedirectToAction("PathFinder", "Identity");
                }


                var users = await _apiService.GetUserList();
                var trackers = await _apiService.GetTrackerListAsync();


                int totalUsers = users?.Count ?? 0;
                int apiUsers = users?.Count(u => u.APIUser) ?? 0;
                int normalUsers = totalUsers - apiUsers;

                int totalDevices = trackers?.Count ?? 0;
                int activeDevices = trackers?.Count(t => t.Enabled) ?? 0;
                int inactiveDevices = totalDevices - activeDevices;


                var recentDevices = trackers?
                    .OrderByDescending(t => t.UTCLastSet)
                    .Take(5)
                    .Select(t => new
                    {
                        Name = t.DisplayName,
                        Phone = t.PhoneNumber,
                        Date = t.UTCLastSet
                    })
                    .ToList();


                var recentUsers = users?
                    .OrderByDescending(u => u.Id)
                    .Take(5)
                    .Select(u => new
                    {
                        Name = $"{u.FirstName} {u.LastName}",
                        u.LoginId
                    })
                    .ToList();


                var recentActivities = await _apiService.GetRecentActivityAsync(10);




                ViewBag.TotalUsers = totalUsers;
                ViewBag.APIUsers = apiUsers;
                ViewBag.NormalUsers = normalUsers;
                ViewBag.TotalDevices = totalDevices;
                ViewBag.ActiveDevices = activeDevices;
                ViewBag.InactiveDevices = inactiveDevices;
                ViewBag.RecentDevices = recentDevices;
                ViewBag.RecentUsers = recentUsers;
                ViewBag.RecentActivities = recentActivities;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Toast"] = $"Error#Failed to load dashboard: {ex.Message}";
                return RedirectToAction("Index");
            }
        }




        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult AddDevice()
        {
            return View(new Tracker());
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddDevices()
        {
            try
            {
                string imei = Request.Form["IMEI"];
                string serialNo = Request.Form["SerialNo"];
                string phoneNumber = Request.Form["PhoneNumber"];
                string displayName = Request.Form["DisplayName"];
                string enabledStr = Request.Form["Enabled"];
                string utcLastSetStr = Request.Form["UTCLastSet"];
                string addedByLoginId = Request.Form["AddedByLoginId"];


                if (string.IsNullOrWhiteSpace(imei) ||
                    string.IsNullOrWhiteSpace(serialNo) ||
                    string.IsNullOrWhiteSpace(phoneNumber) ||
                    string.IsNullOrWhiteSpace(displayName) ||
                    string.IsNullOrWhiteSpace(enabledStr) ||
                    string.IsNullOrWhiteSpace(utcLastSetStr) ||
                    string.IsNullOrWhiteSpace(addedByLoginId))
                {
                    ViewBag.Error = "Please fill in all required fields.";
                    return View("AddDevice");
                }

                bool enabled = enabledStr == "true";

                if (!DateTime.TryParse(utcLastSetStr, out DateTime utcLastSet))
                {
                    ViewBag.Error = "Invalid date/time format.";
                    return View("AddDevice");
                }


                var users = await _apiService.GetUserList();
                var validUser = users.FirstOrDefault(u =>
                    u.LoginId.Equals(addedByLoginId, StringComparison.OrdinalIgnoreCase));

                if (validUser == null)
                {
                    ViewBag.Error = "Please enter a valid user login ID.";
                    return View("AddDevice");
                }

                var tracker = new Tracker
                {
                    IMEI = imei,
                    SerialNo = serialNo,
                    PhoneNumber = phoneNumber,
                    DisplayName = displayName,
                    Enabled = enabled,
                    UTCLastSet = utcLastSet,
                    AddedByLoginId = addedByLoginId
                };

                await _apiService.AddTrackerAsync(tracker);

                TempData["Message"] = "Tracker added successfully!";
                return RedirectToAction("DeviceList", "Admin");
            }
            catch (Exception ex)
            {

                ViewBag.Error = $"Error adding device: {ex.Message}";
                return View("AddDevice");
            }
        }



        [Authorize(Roles = "Admin")]
        public IActionResult EditUser()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AddUser()
        {
            return View();
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] UserProfile newUser)
        {
            try
            {
                if (newUser == null)
                {
                    throw new ArgumentNullException(nameof(newUser));
                }

                string createdUserId = await _apiService.AddUserAsync(newUser);

                TempData["Message"] = "User added successfully!";
                return Json(new { success = true, id = createdUserId, message = "User added successfully!" });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error adding user: {ex.Message}";
                return Json(new { success = false, message = ex.Message });
            }
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditDevice(int id)
        {
            try
            {

                var tracker = await _apiService.GetTrackerByIdAsync(id);

                if (tracker == null)
                {
                    TempData["Message"] = "Error#Tracker not found.";
                    return RedirectToAction("DeviceList");
                }


                return View(tracker);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error#Failed to load tracker: {ex.Message}";
                return RedirectToAction("DeviceList");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditDevice([FromForm] Tracker updatedTracker)
        {
            try
            {
                if (updatedTracker == null || updatedTracker.Id == 0)
                {
                    TempData["Message"] = "Error#Invalid tracker data provided.";
                    return RedirectToAction("DeviceList");
                }

                var tracker = await _apiService.UpdateTrackerAsync(updatedTracker.Id, updatedTracker);

                TempData["Message"] = "Tracker updated successfully!";
                return RedirectToAction("DeviceList");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error#Failed to update tracker: {ex.Message}";
                return RedirectToAction("DeviceList");
            }
        }


        [HttpPost]
        [Route("DeleteDevice")]
        public async Task<IActionResult> DeleteDevice([FromQuery] int id)
        {
            try
            {
                bool result = await _apiService.DeleteTrackerAsync(id);

                if (result)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Tracker deleted successfully"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to delete tracker"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LiveMap(int? trackerId = null)
        {
            try
            {
                if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Admin"))
                {
                    return RedirectToAction("PathFinder", "Identity");
                }

                List<Location> locations = new List<Location>();
                string jwtToken = User.FindFirst("jwt")?.Value;

                if (trackerId.HasValue)
                {
                    locations = await _apiService.GetTrackerLocationsAsync(trackerId.Value, jwtToken);
                }
                else
                {
                    try
                    {
                        locations = await _apiService.GetAllTrackerLocationsAsync(jwtToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching all tracker locations: {ex.Message}");
                        var trackers = await _apiService.GetTrackerListAsync();
                        foreach (var tracker in trackers)
                        {
                            var trackerLocations = await _apiService.GetTrackerLocationsAsync(tracker.Id, jwtToken);
                            locations.AddRange(trackerLocations);
                        }
                    }
                }

                return View(locations);
            }
            catch (Exception ex)
            {
                TempData["Toast"] = $"Error#Failed to load tracker locations: {ex.Message}";
                Console.WriteLine($"Error fetching location data: {ex.Message}");
                return View(new List<Location>());
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeviceList()
        {
            try
            {
                var trackers = await _apiService.GetTrackerListAsync();
                System.Console.WriteLine($"\n\n\n\n\nFetched {trackers.Count} trackers from API.");
                return View(trackers);
            }
            catch (Exception ex)
            {
                TempData["Toast"] = $"Error#Failed to load trackers: {ex.Message}";
                return View(new List<Tracker>());
            }
        }


        [Authorize(Roles = "Admin")]
        public IActionResult Settings()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UserTracker(int? userId = null)
        {
            try
            {
                string jwtToken = User.FindFirst("jwt")?.Value;
                var users = await _apiService.GetUserList();
                ViewBag.Users = users;
                ViewBag.SelectedUserId = userId;

                List<Tracker> trackers = new List<Tracker>();

                if (userId.HasValue)
                {
                    // Pass int userId to the API service instead of Guid
                    trackers = await _apiService.GetTrackersByUserAsync(userId.Value, jwtToken);
                    ViewBag.Message = trackers.Any() ? null : "No trackers assigned to this user.";
                }
                else
                {
                    // If no user selected, you can optionally fetch all trackers or handle differently
                    trackers = await _apiService.GetUserTrackersAsync();
                }

                return View(trackers);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unable to fetch trackers: {ex.Message}";
                return View(new List<Tracker>());
            }
        }
    }

        public class AddUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string LoginId { get; set; }
        public bool APIUser { get; set; }
    }
}