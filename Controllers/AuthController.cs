using E_Commerce.DTOs.Auth;
using E_Commerce.Services.Authservice;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // Request OTP
        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp(RequestOtpDto dto)
        {
            var success = await _authService.RequestOtpAsync(dto);
            if (!success)
                return BadRequest("Invalid email");

            return Ok("OTP sent successfully");
        }

        // Verify OTP
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
        {
            var authResponse = await _authService.VerifyOtpAsync(dto);
            if (authResponse == null)
                return BadRequest("Invalid OTP or Email");

            return Ok(authResponse);
        }
    }
}
