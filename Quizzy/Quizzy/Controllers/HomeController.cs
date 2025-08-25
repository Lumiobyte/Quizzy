using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Quizzy.Models;

namespace Quizzy.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult CreateQuiz(int id = -1, string? import = null)
        {
            QuizCreatorModel quiz;

            if (id > 0)
            {
                quiz = new QuizCreatorModel(id);
            }
            else if (!string.IsNullOrEmpty(import))
            {
                var json = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(Uri.UnescapeDataString(import))
                );
                quiz = new QuizCreatorModel(json);
            }
            else
            {
                quiz = new QuizCreatorModel();
            }

            return View(quiz);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
