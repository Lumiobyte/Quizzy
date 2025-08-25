using Microsoft.AspNetCore.Mvc;
using Quizzy.Models;

namespace Quizzy.Controllers
{
    [ApiController]
    [Route("QuizCreator")]
    public class QuizCreatorController : ControllerBase
    {
        [HttpPost("Create")]
        public IActionResult Create([FromBody] QuizCreatorModel model, [FromQuery] bool createNew = true)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Logic here

            return Ok(new { success = true, message = "Quiz saved." });
        }
    }
}