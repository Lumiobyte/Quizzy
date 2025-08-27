using Microsoft.AspNetCore.Mvc;

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
    }
}