using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DR_Repo.Auth;

public class AuthService
{
    private readonly List<AuthUser> _users;
    private readonly JwtSettings _settings;
    private readonly ConcurrentDictionary<string, byte> _revokedTokens = new();

    public AuthService(IConfiguration configuration, IOptions<JwtSettings> jwtOptions)
    {
        _settings = jwtOptions.Value;
        _users = configuration.GetSection("Auth:Users").Get<List<AuthUser>>() ?? new List<AuthUser>();
    }

    public AuthResponse? SignIn(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var user = _users.FirstOrDefault(u =>
            string.Equals(u.Username, request.Username, StringComparison.OrdinalIgnoreCase) &&
            u.Password == request.Password);

        if (user is null)
        {
            return null;
        }

        return CreateToken(user);
    }

    public void SignOut(string? jti)
    {
        if (!string.IsNullOrWhiteSpace(jti))
        {
            _revokedTokens.TryAdd(jti, 0);
        }
    }

    public bool IsRevoked(string? jti)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return false;
        }

        return _revokedTokens.ContainsKey(jti);
    }

    private AuthResponse CreateToken(AuthUser user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_settings.ExpiresMinutes);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Username = user.Username,
            Role = user.Role,
            ExpiresAtUtc = expires
        };
    }
}
