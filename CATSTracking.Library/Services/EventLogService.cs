using System;
using System.Threading.Tasks;
using CATSTracking.Library.Data;
using CATSTracking.Library.Models;

namespace CATSTracking.Library.Services
{
    public class EventLogService
    {
        private readonly CATSContext _context;

        public EventLogService(CATSContext context)
        {
            _context = context;
        }

        public async Task LogEventAsync(string tag, string message, string? loginid = null)
        {
            // Event logging is just best effort. So if we get bad input we drop the event
            if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            try
            {
                EventLog eventLogEntry = new EventLog
                {
                    Tag = tag,
                    Message = message,
                    LoginId = loginid,
                    UTCDateTime = DateTime.UtcNow
                };

                await _context.EventLogs.AddAsync(eventLogEntry);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log event: {ex.Message}");
            }
        }
    }
}