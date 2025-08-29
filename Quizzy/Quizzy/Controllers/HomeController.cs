using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Quizzy.Core.Repositories;
using Quizzy.Models;

namespace Quizzy.Controllers
{
    public class HomeController(IUnitOfWork repository) : Controller
    {
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

        public IActionResult QuizSelector()
        {
            return View();
        }

        public IActionResult CreateQuiz(Guid? id = null, string? import = null)
        {
            QuizCreatorModel model;

            if (id is not null)
            {
                // Load quiz with author/questions/answers to prefill
                var quizzes = repository.Quizzes.GetAllWithDetailsAsync().GetAwaiter().GetResult();
                var quiz = quizzes.FirstOrDefault(q => q.Id == id.Value);
                model = quiz is not null
                    ? new QuizCreatorModel(quiz)
                    : new QuizCreatorModel();
            }
            else if (!string.IsNullOrEmpty(import))
            {
                var json = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(Uri.UnescapeDataString(import))
                );
                model = new QuizCreatorModel(json);
            }
            else model = new QuizCreatorModel();

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
