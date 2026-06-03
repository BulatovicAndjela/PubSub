using Microsoft.AspNetCore.Mvc;
using PubSub.services;

namespace PubSub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CupidController : ControllerBase
    {
        private readonly ICupidService _cupidService;

        public CupidController(ICupidService cupidService)
        {
            _cupidService = cupidService;
        }

        [HttpPost("send-letters")]
        public IActionResult SendLetters()
        {
            try
            {
                _cupidService.SendLetters();
                return Ok(new { message = "Pisma su poslata!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
