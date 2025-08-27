using Microsoft.AspNetCore.Mvc;
using Quizzy.Core.Repositories;

namespace Quizzy.Controllers
{
    [ApiController]
    [Route("QuizSelector")]
    public class QuizSelectorController(IUnitOfWork repository) : ControllerBase
    {
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var quizzes = await repository.Quizzes.GetAllAsync();
            return Ok(quizzes.ToList());
        }

        [HttpGet("GetAllById")]
        public async Task<IActionResult> GetAllById([FromQuery] Guid id)
        {
            var quizzes = await repository.Quizzes.GetAllAsync();
            return Ok(quizzes.Where(q => q.QuizAuthorId == id).ToList());
        }

        [HttpGet("GetAllByName")]
        public async Task<IActionResult> GetAllByName([FromQuery] string name)
        {
            var quizzes = await repository.Quizzes.GetAllAsync();
            var term = name?.ToLowerInvariant() ?? string.Empty;
            return Ok(quizzes.Where(q => q.Title != null && q.Title.ToLowerInvariant().Contains(term)).ToList());
        }

        [HttpGet("GetAllByIdAndName")]
        public async Task<IActionResult> GetAllByIdAndName([FromQuery] Guid id, [FromQuery] string name)
        {
            var term = name?.ToLowerInvariant() ?? string.Empty;
            var quizzes = await repository.Quizzes.GetAllAsync();
            return Ok(quizzes
                .Where(q => q.QuizAuthorId == id)
                .Where(q => q.Title != null && q.Title.ToLowerInvariant().Contains(term))
                .ToList());
        }
    }
}