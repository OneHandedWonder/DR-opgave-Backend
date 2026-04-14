using System.IdentityModel.Tokens.Jwt;
using DR_Repo.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DR_Repo.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("signin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AuthResponse> SignIn([FromBody] LoginRequest request)
    {
        var token = _authService.SignIn(request);
        if (token is null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        return Ok(token);
    }

    [Authorize]
    [HttpPost("signout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SignOutCurrentSession()
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrWhiteSpace(jti))
        {
            return BadRequest(new { message = "Current token is missing the required jti claim." });
        }
        _authService.SignOut(jti);

        return Ok(new { message = "Signed out. Token is revoked." });
    }
}
