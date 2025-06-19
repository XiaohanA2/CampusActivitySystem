using CampusActivity.Shared.DTOs;

namespace CampusActivity.BlazorWeb.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
    Task LogoutAsync();
    Task<UserDto?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
} 