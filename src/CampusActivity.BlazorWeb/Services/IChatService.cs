using System.Threading.Tasks;

namespace CampusActivity.BlazorWeb.Services
{
    public interface IChatService
    {
        Task<string> SendMessageAsync(string message, string? userId = null);
        Task<bool> IsAvailableAsync();
    }
} 