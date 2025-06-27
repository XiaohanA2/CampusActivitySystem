using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

namespace CampusActivity.BlazorWeb.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            
            if (string.IsNullOrWhiteSpace(token))
                return new AuthenticationState(_anonymous);

            // 验证token是否过期
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                await _localStorage.RemoveItemAsync("authToken");
                await _localStorage.RemoveItemAsync("user");
                return new AuthenticationState(_anonymous);
            }

            // 从token中提取用户信息并修复角色声明
            var claims = new List<Claim>();
            foreach (var claim in jwtToken.Claims)
            {
                if (claim.Type == "role")
                {
                    // 将小写的"role"转换为标准的角色声明类型
                    claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                }
                else if (claim.Type == "unique_name")
                {
                    // 将"unique_name"转换为标准的名称声明类型
                    claims.Add(new Claim(ClaimTypes.Name, claim.Value));
                }
                else if (claim.Type == "nameid")
                {
                    // 将"nameid"转换为标准的名称标识符声明类型
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, claim.Value));
                }
                else
                {
                    claims.Add(claim);
                }
            }
            
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch (InvalidOperationException)
        {
            // 预渲染阶段无法访问localStorage，返回匿名状态
            return new AuthenticationState(_anonymous);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task NotifyUserAuthentication(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        // 修复角色声明映射
        var claims = new List<Claim>();
        foreach (var claim in jwtToken.Claims)
        {
            if (claim.Type == "role")
            {
                claims.Add(new Claim(ClaimTypes.Role, claim.Value));
            }
            else if (claim.Type == "unique_name")
            {
                claims.Add(new Claim(ClaimTypes.Name, claim.Value));
            }
            else if (claim.Type == "nameid")
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, claim.Value));
            }
            else
            {
                claims.Add(claim);
            }
        }
        
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        var authState = new AuthenticationState(user);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    public async Task NotifyUserLogout()
    {
        var authState = new AuthenticationState(_anonymous);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }
} 