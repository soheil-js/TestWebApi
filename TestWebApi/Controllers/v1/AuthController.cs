using Asp.Versioning;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSec.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TestWebApi.Data;
using TestWebApi.Dtos;
using TestWebApi.Models;
using TestWebApi.Services;

namespace TestWebApi.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AuthController : ControllerBase
    {
        private readonly TestDbContext _context;
        private readonly IConfiguration _config;
        private readonly PasswordHasher _passwordHasher;
        private readonly IValidator<UserDto> _authValidator;

        public AuthController(TestDbContext context, IConfiguration config, PasswordHasher passwordHasher, IValidator<UserDto> authValidator)
        {
            _context = context;
            _config = config;
            _passwordHasher = passwordHasher;
            _authValidator = authValidator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto dto)
        {
            ValidationResult result = await _authValidator.ValidateAsync(dto);
            if (!result.IsValid)
            {
                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("User already exists");

            var hashed = _passwordHasher.HashPassword(dto.Password);

            var user = new User
            {
                Username = dto.Username,
                Password = hashed,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok("User registered");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null) return Unauthorized("Invalid username or password");

            if (!_passwordHasher.VerifyPassword(dto.Password, user.Password))
                return Unauthorized("Invalid username or password");

            var refreshToken = CreatedRefreshToken();
            user.RefreshToken = refreshToken.token;
            user.RefreshTokenExpiry = refreshToken.expired;
            await _context.SaveChangesAsync();

            var aceessToken = CreateToken(user);
            return Ok(new 
            { 
                status = "ok",
                username = user.Username,
                access_token = aceessToken.token,
                refresh_token = refreshToken.token,
                expiresIn = aceessToken.expired
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshDto token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == token.refresh_token);
            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                return Unauthorized("Invalid or expired refresh token");

            var refresh = CreatedRefreshToken();

            user.RefreshToken = refresh.token;
            user.RefreshTokenExpiry = refresh.expired;
            await _context.SaveChangesAsync();

            var aceessToken = CreateToken(user);
            return Ok(new
            {
                status = "ok",
                username = user.Username,
                access_token = aceessToken.token,
                refresh_token = refresh.token,
                expiresIn = aceessToken.expired
            });
        }

        private (string token, DateTime expired) CreatedRefreshToken()
        {
            var refreshToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
            return (refreshToken, DateTime.UtcNow.AddDays(7));
        }

        private (string token, int expired) CreateToken(User user)
        {
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var expireMinutes = _config.GetValue<int>("Jwt:ExpireMinutes");
            var tokenExpiryTimeStamp = DateTime.UtcNow.AddMinutes(expireMinutes);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                }),
                Expires = tokenExpiryTimeStamp,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(HashAlgorithm.Sha256.Hash(keyBytes)), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(securityToken);

            return (accessToken, (int)tokenExpiryTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds);
        }
    }
}
