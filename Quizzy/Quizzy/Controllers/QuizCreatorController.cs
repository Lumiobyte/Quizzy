using Microsoft.AspNetCore.Mvc;
using Quizzy.Core.DTOs;
using Quizzy.Core.Repositories;
using Quizzy.Core.Services;

namespace Quizzy.Controllers
{
    [ApiController]
    [Route("QuizCreator")]
    public class QuizCreatorController(IAIQuizGeneratorService aiQuizGeneratorService, IQuizCreationService quizCreationService, IUnitOfWork repository) : ControllerBase
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

        [HttpPost("AIGenerate")]
        public async Task<IActionResult> AIGenerate([FromBody] string prompt, [FromQuery] Guid uId)
        {

            if (await repository.UserAccounts.GetByIdAsync(uId) is not { } userAccount)
            {
                return BadRequest(new { message = "User not found" });
            }

            var createdQuiz = await aiQuizGeneratorService.AIGenerateQuiz(prompt);

            createdQuiz.QuizAuthor = userAccount;

            await repository.Quizzes.AddAsync(createdQuiz);
            await repository.SaveChangesAsync();

            return Ok(new { message = $"Quiz '{createdQuiz.Title}' saved! Check the quiz list to edit or play." });
        }
    }
}