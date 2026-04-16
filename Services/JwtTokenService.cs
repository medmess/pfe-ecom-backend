using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using pfe.ecom.api.Models;

namespace pfe.ecom.api.Services;

public class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(ApplicationUser user, IList<string>? roles)
    {
        var jwtKey = _config["Jwt:Key"];
        var jwtIssuer = _config["Jwt:Issuer"];

        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("JWT Key is missing.");

        if (string.IsNullOrWhiteSpace(jwtIssuer))
            throw new InvalidOperationException("JWT Issuer is missing.");

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new Claim(ClaimTypes.Name, user.Email ?? string.Empty),
            new Claim("fullName", user.FullName ?? string.Empty),
            new Claim("accountType", user.AccountType ?? string.Empty)
        };

        if (roles != null)
        {
            foreach (var role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}