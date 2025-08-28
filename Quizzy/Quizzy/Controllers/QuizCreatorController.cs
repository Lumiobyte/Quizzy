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
        public async Task<IActionResult> Create([FromBody] QuizCreatorModel model, [FromQuery] Guid uId, [FromQuery] bool createNew = true)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await quizCreationService.GenerateQuiz(model, uId, createNew);
            return Ok(new { message = "Quiz saved." });
        }
    }
}