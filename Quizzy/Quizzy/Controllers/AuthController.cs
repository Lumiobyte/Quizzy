using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Quizzy.Models;
using Quizzy.Core.Services;

namespace Quizzy.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            int id = AuthenticationService.Instance.LoginUser(request.Username, request.Password);

            return Ok(new {id});
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            string message = "uh oh";
            int? id = null;
            try
            {
                id = AuthenticationService.Instance.CreateNewUser(request.Username, request.Password, request.Email);
            }
            catch (ArgumentException ex)
            {
                message = ex.Message;
            }

            return Ok(new { id, message });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
    }
}
