using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using CampusActivity.Domain.Entities;
using CampusActivity.Infrastructure.UnitOfWork;
using CampusActivity.Shared.DTOs;
using CampusActivity.Shared.Constants;

namespace CampusActivity.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IConfiguration configuration,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => 
                u.Username == loginDto.Username && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            // 更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var userDto = _mapper.Map<UserDto>(user);

            return new AuthResponseDto
            {
                Token = token,
                Expires = DateTime.UtcNow.AddHours(24),
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登录失败: {Username}", loginDto.Username);
            throw;
        }
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            // 检查用户名是否已存在
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => 
                u.Username == registerDto.Username || u.Email == registerDto.Email);

            if (existingUser != null)
            {
                throw new InvalidOperationException("用户名或邮箱已存在");
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                Phone = registerDto.Phone,
                Role = registerDto.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var userDto = _mapper.Map<UserDto>(user);

            return new AuthResponseDto
            {
                Token = token,
                Expires = DateTime.UtcNow.AddHours(24),
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户注册失败: {Username}", registerDto.Username);
            throw;
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        return user != null ? _mapper.Map<UserDto>(user) : null;
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user != null ? _mapper.Map<UserDto>(user) : null;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<UserDto> UpdateUserAsync(int id, UserDto userDto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException("用户不存在");
        }

        // 更新允许的字段
        user.FullName = userDto.FullName;
        user.Email = userDto.Email;
        user.Phone = userDto.Phone;
        user.Avatar = userDto.Avatar;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        await _unitOfWork.Users.DeleteAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("当前密码错误");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<UserDto> UpdateProfileAsync(int userId, UserDto userDto)
    {
        return await UpdateUserAsync(userId, userDto);
    }

    public async Task<bool> SetUserActiveStatusAsync(int userId, bool isActive)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.IsActive = isActive;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "DefaultSecretKey");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("FullName", user.FullName)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
} 