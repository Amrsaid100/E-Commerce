using E_Commerce.DTOs.Auth;
using E_Commerce.Services.Authservice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;

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

        // ================= OTP =================

        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp(RequestOtpDto dto)
        {
            try
            {
                var ok = await _authService.RequestOtpAsync(dto);
                if (!ok) return BadRequest(new { message = "Invalid email." });

                return Ok(new { message = "OTP sent." });
            }
            catch (InvalidOperationException ex)
            {
                // SMTP/config issues
                return StatusCode(502, new { message = ex.Message });
            }
            catch (SmtpException)
            {
                return StatusCode(502, new { message = "Email provider rejected authentication. Check SMTP credentials (App Password for Gmail)." });
            }
        }


        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
        {
            var authResponse = await _authService.VerifyOtpAsync(dto);
            if (authResponse == null)
                return BadRequest("Invalid OTP or Email");

            return Ok(authResponse);
        }

        // ================= REFRESH TOKEN =================

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
        {
            var result = await _authService.RefreshAsync(dto);
            if (result == null)
                return Unauthorized("Invalid refresh token");

            return Ok(result);
        }

        // ================= LOGOUT (REVOKE) =================

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // userId is in "sub"
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

            if (string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(jti) || string.IsNullOrWhiteSpace(expClaim))
                return Unauthorized();

            if (!int.TryParse(sub, out var userId))
                return Unauthorized("Invalid user id in token");

            var expUnix = long.Parse(expClaim);
            var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;

            var success = await _authService.LogoutAsync(userId, jti, expiresAtUtc);

            return success
                ? Ok(new { message = "Logged out successfully" })
                : BadRequest("Logout failed");
        }


        // ================= ROLES =================

        [HttpPost("promote")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> PromoteUser([FromBody] PromoteUserDto dto)
        {
            var ownerEmail =
                User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ??
                User.FindFirst(ClaimTypes.Email)?.Value;


            if (string.IsNullOrWhiteSpace(ownerEmail))
                return Unauthorized();

            var success = await _authService
                .PromoteUserToAdminAsync(ownerEmail, dto.Email);

            if (!success)
                return BadRequest("Cannot promote user");

            return Ok("User promoted to Admin");
        }

        [HttpPost("demote")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DemoteAdmin([FromBody] DemoteAdminDto dto)
        {
            var ownerEmail =
                User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ??
                User.FindFirst(ClaimTypes.Email)?.Value;


            if (string.IsNullOrWhiteSpace(ownerEmail))
                return Unauthorized();

            var success = await _authService
                .DemoteAdminToUserAsync(ownerEmail, dto.Email);

            if (!success)
                return BadRequest("Cannot demote admin");

            return Ok("Admin demoted to User");
        }
    }
}
