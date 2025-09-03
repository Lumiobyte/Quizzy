using Microsoft.AspNetCore.Mvc;
using Quizzy.Core.Repositories;
using Quizzy.Core.Services;

namespace Quizzy.Web.Controllers
{
    public class GameController : Controller
    {
        [HttpGet("/Host")]
        public IActionResult Host() => View();

        [HttpGet("/Player")]
        public IActionResult Player() => View();

        [HttpGet("/Host/Lobby")]
        public IActionResult HostLobby(string pin)
        {
            ViewData["Pin"] = pin ?? string.Empty;
            return View("Lobby");
        }

        [HttpGet("/Host/Quiz")]
        public IActionResult HostQuiz(string pin)
        {
            ViewData["Pin"] = pin ?? string.Empty;
            return View("Quiz");
        }

        [HttpGet("/Host/Results")]
        public IActionResult HostResults(string pin)
        {
            ViewData["Pin"] = pin ?? string.Empty;
            return View("Results");
        }

        [HttpPost("/Player/SubmitFeedback")]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequest request, [FromServices] IUnitOfWork unitOfWork, [FromServices] EmailService emailService)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SessionPin) || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest();

            var pinUpper = request.SessionPin.Trim().ToUpperInvariant();
            var session = (await unitOfWork.QuizSessions.FindAsync(s => s.GamePin == pinUpper)).FirstOrDefault();
            if (session == null)
                return NotFound();

            var host = await unitOfWork.UserAccounts.GetByIdAsync(session.QuizHostId);
            if (host == null)
                return NotFound();

            var playerName = string.IsNullOrWhiteSpace(request.PlayerName) ? "Anonymous" : request.PlayerName.Trim();
            var subject = $"Quiz Feedback for session {pinUpper}";
            var body = $"Feedback from {playerName}:<br /><br />{request.Message}";
            await emailService.SendEmailAsync(host, subject, body);

            return Ok();
        }
    }

    public class FeedbackRequest
    {
        public string SessionPin { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}