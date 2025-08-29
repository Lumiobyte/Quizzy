using Microsoft.AspNetCore.Mvc;
using Quizzy.Core.Repositories;
using Quizzy.Core.Services;
using Quizzy.Models;

namespace Quizzy.Controllers
{
    [ApiController]
    [Route("QuizCreator")]
    public class QuizCreatorController(IQuizCreationService quizCreationService, IUnitOfWork repository) : ControllerBase
    {
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] QuizCreatorModel model, [FromQuery] Guid uId, [FromQuery] bool createNew = true)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!createNew && model.QuizSourceId is null) return BadRequest("Missing QuizSourceId for update.");
            if (uId == Guid.Empty || await repository.UserAccounts.GetByIdAsync(uId) is null) return BadRequest("Invalid user id.");
            await quizCreationService.GenerateQuiz(model, uId, createNew);
            return Ok(new { message = "Quiz saved." });
        }
    }
}