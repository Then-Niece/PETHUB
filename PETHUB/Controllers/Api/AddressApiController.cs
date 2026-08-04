using Microsoft.AspNetCore.Mvc;
using PETHUB.Helpers;

namespace PETHUB.Controllers.Api
{
    /// <summary>
    /// Provides reusable address information
    /// for client-side dropdowns.
    /// </summary>
    [ApiController]
    [Route("api/address")]
    public class AddressApiController : ControllerBase
    {
        /// <summary>
        /// Returns all available address data.
        /// </summary>
        [HttpGet("locations")]
        public IActionResult GetLocations()
        {
            return Ok(AddressHelper.Locations);
        }
    }
}