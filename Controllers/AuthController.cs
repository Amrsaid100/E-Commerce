using E_Commerce.DTOs.Auth;
using E_Commerce.Services.Authservice;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService auth;
        public AuthController(IAuthService auth)
        {
            this.auth = auth;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto register)
        {
            if (ModelState.IsValid)
            {
                var result = await auth.RegisterAsync(register);
                if (result != null)
                {
                    return Ok(result);
                }
                return Conflict(new { message = "Email already" });
            }
            return BadRequest(ModelState);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto login)
        {
            if (ModelState.IsValid)
            {
                var result = await auth.LoginAsync(login);
                if (result != null)
                {
                    return Ok(result);
                }
                return Unauthorized(new { message = "Invalid email or password" });
            }
            return BadRequest(ModelState);
        }
    }
}
