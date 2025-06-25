using System.ComponentModel.DataAnnotations;

namespace CampusActivity.Shared.DTOs;

public class UserDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(100, ErrorMessage = "姓名长度不能超过100个字符")]
    public string FullName { get; set; } = string.Empty;
    
    public string? Phone { get; set; }
    
    public string? Avatar { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public UserRole Role { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? LastLoginAt { get; set; }
    
    public string? StudentId { get; set; }
    
    public string? Major { get; set; }
    
    public int? Grade { get; set; }
    
    public string? EmployeeId { get; set; }
    
    public string? Department { get; set; }
    
    public string? Title { get; set; }
}

public class LoginDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-50个字符之间")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "姓名不能为空")]
    public string FullName { get; set; } = string.Empty;
    
    public string? Phone { get; set; }
    
    public UserRole Role { get; set; } = UserRole.Student;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public UserDto User { get; set; } = new();
}

public enum UserRole
{
    Student = 1,
    Teacher = 2,
    Admin = 3
} 