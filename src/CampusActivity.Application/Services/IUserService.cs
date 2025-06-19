using CampusActivity.Shared.DTOs;

namespace CampusActivity.Application.Services;

public interface IUserService
{
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto> UpdateUserAsync(int id, UserDto userDto);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<UserDto> UpdateProfileAsync(int userId, UserDto userDto);
    Task<bool> SetUserActiveStatusAsync(int userId, bool isActive);
} 