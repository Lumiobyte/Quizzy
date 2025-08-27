using Microsoft.AspNetCore.Mvc;
using Quizzy.Core.Services;
using Quizzy.Models;

namespace Quizzy.Controllers
{
    [ApiController]
    [Route("QuizCreator")]
    public class QuizCreatorController(IQuizCreationService quizCreationService) : ControllerBase
    {
        [HttpPost("Create")]
        public IActionResult Create([FromBody] QuizCreatorModel model, [FromQuery] bool createNew = true)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Logic here
            quizCreationService.GenerateQuiz();
            return Ok(new { success = true, message = "Quiz saved." });
        }
    }
}