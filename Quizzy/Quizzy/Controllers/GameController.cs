using Microsoft.AspNetCore.Mvc;

namespace Quizzy.Web.Controllers
{
    public class GameController : Controller
    {
        [HttpGet("/Host")]
        public IActionResult Host() => View();

        [HttpGet("/Player")]
        public IActionResult Player() => View();
    }
}