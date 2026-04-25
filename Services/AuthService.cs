using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FlameTrack.API.Models.DTOs;
using FlameTrack.API.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace FlameTrack.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<UserDto> UpdateProfileAsync(string userId, UpdateUserRequestDto request);
}

public class AuthService : IAuthService
{
    private readonly IMongoCollection<UserEntity> _users;
    private readonly string _jwtSecret;

    public AuthService(IMongoClient mongoClient, IConfiguration config)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _users = database.GetCollection<UserEntity>("Users");
        _jwtSecret = config["JwtSecret"] ?? "flametrack_super_secret_key_1234567890_flametrack_web_secure_jwt";
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
        if (existingUser != null)
            throw new Exception("User already exists.");

        var user = new UserEntity
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _users.InsertOneAsync(user);

        return new AuthResponseDto
        {
            Token = GenerateJwtToken(user),
            User = MapToDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid credentials.");

        return new AuthResponseDto
        {
            Token = GenerateJwtToken(user),
            User = MapToDto(user)
        };
    }

    public async Task<UserDto> UpdateProfileAsync(string userId, UpdateUserRequestDto request)
    {
        var filter = Builders<UserEntity>.Filter.Eq(u => u.Id, userId);
        var update = Builders<UserEntity>.Update;
        var updates = new List<UpdateDefinition<UserEntity>>();

        if (request.Name != null) updates.Add(update.Set(u => u.Name, request.Name));
        if (request.Nickname != null) updates.Add(update.Set(u => u.Nickname, request.Nickname));
        if (request.Avatar != null) updates.Add(update.Set(u => u.Avatar, request.Avatar));

        if (!updates.Any()) return MapToDto(await _users.Find(filter).FirstAsync());

        var updatedUser = await _users.FindOneAndUpdateAsync(
            filter, 
            update.Combine(updates), 
            new FindOneAndUpdateOptions<UserEntity> { ReturnDocument = ReturnDocument.After }
        );

        return MapToDto(updatedUser);
    }

    private UserDto MapToDto(UserEntity user) => new()
    {
        Id = user.Id!,
        Name = user.Name,
        Email = user.Email,
        Nickname = user.Nickname,
        Avatar = user.Avatar
    };

    private string GenerateJwtToken(UserEntity user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id!),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
