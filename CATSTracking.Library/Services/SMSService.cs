using Twilio;
using Twilio.Rest.Api.V2010.Account;
using System.Threading.Tasks;
using CATSTracking.Library.Models;
using CATSTracking.Library.Data;


namespace CATSTracking.Library.Services
{
    public class SMSService
    {

        private readonly string _TwilioKey;
        private readonly string _TwilioSecret;
        private readonly string _TwilioPhoneNumber;

        private readonly CATSContext _context;

        //public SMSService(CATSContext context)
        //{
        //    _context = context;

        //    _TwilioKey = Environment.GetEnvironmentVariable("TWILIO_KEY") ?? _TwilioKey;
        //    _TwilioSecret = Environment.GetEnvironmentVariable("TWILIO_SECRET") ?? _TwilioSecret;
        //    _TwilioPhoneNumber = Environment.GetEnvironmentVariable("TWILIO_PHONENUMBER") ?? _TwilioPhoneNumber;

        //    if (string.IsNullOrEmpty(_TwilioKey) || string.IsNullOrEmpty(_TwilioSecret) || string.IsNullOrEmpty(_TwilioPhoneNumber))
        //    {

        //        throw new Exception("Twilio environment variables are not set properly.");
        //    }
        //}

        public List<SMS>? readSMS(string fromNumber)
        {
            try
            {
                List<SMS> smsMessages = _context.SMSes.Where(sms => sms.From == fromNumber).ToList();

                if (smsMessages == null || smsMessages.Count == 0)
                {
                    return null;
                }

                return smsMessages;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public async Task<bool> SendSMSAsync(string toPhoneNumber, string message)
        {

            TwilioClient.Init(_TwilioKey, _TwilioSecret);

            var smsResult = await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(_TwilioPhoneNumber),
            to: new Twilio.Types.PhoneNumber(toPhoneNumber)
            );

            return true;

        }
    }
}