using Microsoft.AspNetCore.Mvc;
using Quizzy.Core.DTOs;
using Quizzy.Core.Repositories;
using Quizzy.Core.Services;

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
            if (uId == Guid.Empty || await repository.UserAccounts.GetByIdAsync(uId) is null) return BadRequest("Invalid user id.");
            try
            {
                if (!createNew)
                {
                    if (model.QuizSourceId is null) return BadRequest("Missing QuizSourceId for update.");
                    await quizCreationService.UpdateQuiz(model, uId);
                }
                else await quizCreationService.GenerateQuiz(model, uId);
                return Ok(new { message = "Quiz saved." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}