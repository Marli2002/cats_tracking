using CATSTracking.Library.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CATSTracking.Library.Services
{
    public class ApiService
    {

        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetToken(TokenRequest tokenRequest)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/token/new", tokenRequest);

            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();

            var result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

            if (result != null && result.TryGetValue("jwt", out var token))
            {
                return token;
            }

            throw new Exception("Token not found in response");
        }


        public async Task<string> ResetUserPassword(Login updatedLoginInfo)
        {

            if (updatedLoginInfo == null)
            {
                throw new ArgumentException("Invalid login information or reset token.");
            }

            System.Console.WriteLine($"Resetting password for user: {updatedLoginInfo}");
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/user/password", updatedLoginInfo);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

            if (result != null && result.TryGetValue("id", out var loginId))
            {
                return loginId;
            }

            throw new Exception("Failed to reset password.");
        }

        public async Task<List<Tracker>> GetTrackerListAsync()
        {
            var response = await _httpClient.GetAsync("/api/v1/tracker/list");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var trackers = System.Text.Json.JsonSerializer.Deserialize<List<Tracker>>(jsonString, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            });

            return trackers ?? new List<Tracker>();
        }

        public async Task<Tracker> AddTrackerAsync(Tracker newTracker)
        {
            if (newTracker == null)
                throw new ArgumentNullException(nameof(newTracker));

            var response = await _httpClient.PostAsJsonAsync("/api/v1/tracker/new", newTracker);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var createdTracker = System.Text.Json.JsonSerializer.Deserialize<Tracker>(jsonString);

            if (createdTracker == null)
                throw new Exception("Failed to create tracker.");

            return createdTracker;
        }

        public async Task<string> AddUserAsync(UserProfile newUser)
        {
            if (newUser == null)
            {
                return null;
            }

            var response = await _httpClient.PostAsJsonAsync("/api/v1/user/new", newUser);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

            if (result != null && result.TryGetValue("id", out var userId))
            {
                return userId;
            }

            return null;
        }


        public async Task<List<UserProfile>> GetUserList()
        {
            var response = await _httpClient.GetAsync("/api/v1/user");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var users = System.Text.Json.JsonSerializer.Deserialize<List<UserProfile>>(jsonString, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            });

            if (users == null)
                throw new Exception("Failed to retrieve user list.");

            return users ?? new List<UserProfile>();
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to delete user. Status Code: {response.StatusCode}, Error: {errorContent}");
            }
        }

        public async Task<bool> DeleteTrackerAsync(int trackerId)
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/tracker/{trackerId}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to delete tracker. Status Code: {response.StatusCode}, Error: {errorContent}");
            }
        }


        public async Task<List<Location>> GetTrackerLocationsAsync(int id)
        {
            var response = await _httpClient.GetAsync($"/api/v1/Tracker/{id}/openstreetmapactivity");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var locations = System.Text.Json.JsonSerializer.Deserialize<List<Location>>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
            });

            return locations ?? new List<Location>();
        }

        public async Task<List<Location>> GetTrackerLocationsAsync(int id, string jwtToken = null)
        {
            if (!string.IsNullOrEmpty(jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            }
            var response = await _httpClient.GetAsync($"/api/v1/Tracker/{id}/openstreetmapactivity");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var locations = System.Text.Json.JsonSerializer.Deserialize<List<Location>>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
            });

            return locations ?? new List<Location>();
        }

        public async Task<List<Location>> GetAllTrackerLocationsAsync(string jwtToken = null)
        {
            if (!string.IsNullOrEmpty(jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            }
            var response = await _httpClient.GetAsync("/api/v1/Tracker/all/openstreetmapactivity");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var locations = System.Text.Json.JsonSerializer.Deserialize<List<Location>>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
            });

            return locations ?? new List<Location>();
        }
        public async Task<Tracker?> GetTrackerByIdAsync(int trackerId)
        {
            var response = await _httpClient.GetAsync($"/api/v1/tracker/{trackerId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get tracker (ID: {trackerId}). Status: {response.StatusCode}, Error: {errorContent}");
            }

            var jsonString = await response.Content.ReadAsStringAsync();

            var tracker = System.Text.Json.JsonSerializer.Deserialize<Tracker>(
                jsonString,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                });

            return tracker;
        }
        
        public async Task<List<Tracker>> GetUserTrackersAsync()
        {
            var response = await _httpClient.GetAsync("api/v1/tracker/userlist");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Tracker>>(json);
        }


        public async Task<List<Tracker>> GetTrackersByUserAsync(int userId, string jwtToken = null)
        {
            if (!string.IsNullOrEmpty(jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            }

            var response = await _httpClient.GetAsync($"/api/v1/tracker/assigned/{userId}");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var trackers = System.Text.Json.JsonSerializer.Deserialize<List<Tracker>>(jsonString, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            });

            return trackers ?? new List<Tracker>();
        }



        public async Task<Tracker> UpdateTrackerAsync(int trackerId, Tracker updatedTracker)
        {
            if (updatedTracker == null)
                throw new ArgumentNullException(nameof(updatedTracker));

            var response = await _httpClient.PutAsJsonAsync($"/api/v1/tracker/{trackerId}/edit", updatedTracker);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<Tracker>(jsonString, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? updatedTracker;
        }
       
        public async Task<object> GetUserDashboardAsync()
        {
            var response = await _httpClient.GetAsync("/api/v1/tracker/dashboard");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var dashboardData = System.Text.Json.JsonSerializer.Deserialize<object>(
                jsonString,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                });

            return dashboardData ?? new object();
        }

        public async Task<List<ActivityLog>> GetRecentActivityAsync(int count = 10)
        {
            var response = await _httpClient.GetAsync($"/api/v1/user/recent?count={count}");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var activities = JsonConvert.DeserializeObject<List<ActivityLog>>(jsonString);

            return activities ?? new List<ActivityLog>();
        }

    }
}