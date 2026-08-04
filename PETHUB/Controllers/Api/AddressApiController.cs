using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
        // Stores the application's root directory.
        // This is used to locate the Data folder.
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// Receives the hosting environment through
        /// dependency injection.
        /// </summary>
        public AddressApiController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Returns all available address data
        /// from Data/philippines.json.
        /// </summary>
        [HttpGet("locations")]
        public IActionResult GetLocations()
        {
            // Build the absolute path to the JSON file.
            var filePath = Path.Combine(
                _environment.ContentRootPath,
                "Data",
                "philippines.json"
            );

            // Check whether the file exists.
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("philippines.json not found.");
            }

            // Read the JSON file.
            var json = System.IO.File.ReadAllText(filePath);

            // Deserialize it into the same structure
            // your JavaScript already expects.
            var locations = JsonSerializer.Deserialize<
                Dictionary<string, Dictionary<string, List<string>>>
            >(json);

            // Return the JSON to the client.
            return Ok(locations);
        }
    }
}