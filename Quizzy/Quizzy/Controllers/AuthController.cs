using Microsoft.AspNetCore.Mvc;
using Quizzy.Core.Services;
using Quizzy.Core.Repositories;

namespace Quizzy.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController(ILoginService loginService, IUnitOfWork repository) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var id = await loginService.LoginUser(request.Username, request.Password);

            return Ok(new {id});
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            string message = "An unknown error occured";
            Guid? id = null;
            try
            {
                id = await loginService.CreateNewUser(request.Username, request.Password, request.Email);
            }
            catch (ArgumentException ex)
            {
                message = ex.Message;
            }

            return Ok(new { id, message });
        }

        [HttpPost("checkIdInDb")]
        public async Task<IActionResult> Login([FromBody] Guid id)
        {
            var user = await repository.UserAccounts.GetByIdAsync(id);
            if (user == null) return BadRequest("Id not found in database");
            return Ok();
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
