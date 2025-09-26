using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace TestWebApi.Controllers.v1
{

    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult IsOk()
        {
            return Ok(new { status = "ok", message = "Welcome to Test API. Visit '/docs' for documentation." });
        }
    }
}
