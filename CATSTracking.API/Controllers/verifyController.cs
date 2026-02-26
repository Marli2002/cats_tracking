using CATSTracking.Library.Data;
using CATSTracking.Library.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CATSTracking.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class verifyController : ControllerBase
    {

        private readonly CATSContext _context;

        public verifyController(CATSContext context)
        {
            _context = context;
        }

    }
}
