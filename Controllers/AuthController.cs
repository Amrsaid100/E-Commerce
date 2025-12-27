using E_Commerce.DTOs.Auth;
using E_Commerce.Services.Authservice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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

        // Promote User to Admin (Owner only)
        [HttpPost("promote")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> PromoteUser([FromBody] PromoteUserDto dto)
        {
            var ownerEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            if (string.IsNullOrWhiteSpace(ownerEmail)) return Unauthorized();

            var success = await _authService.PromoteUserToAdminAsync(ownerEmail, dto.Email);
            if (!success) return BadRequest("Cannot promote user");

            return Ok("User promoted to Admin");
        }

        // Demote Admin to User (Owner only)
        [HttpPost("demote")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DemoteAdmin([FromBody] DemoteAdminDto dto)
        {
            var ownerEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            if (string.IsNullOrWhiteSpace(ownerEmail)) return Unauthorized();

            var success = await _authService.DemoteAdminToUserAsync(ownerEmail, dto.Email);
            if (!success) return BadRequest("Cannot demote admin");

            return Ok("Admin demoted to User");
        }
    }
}
