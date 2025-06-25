using System.Threading.Tasks;

namespace CampusActivity.Shared.DTOs;

public interface IUserContextService
{
    Task<UserContextDto> GetUserContextAsync(int userId);
    Task<string> GetUserContextSummaryAsync(int userId);
} 