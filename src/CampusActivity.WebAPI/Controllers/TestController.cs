using Microsoft.AspNetCore.Mvc;

namespace CampusActivity.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public ActionResult<object> Get()
    {
        return Ok(new
        {
            Message = "API连接正常",
            Timestamp = DateTime.Now,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            Assembly = typeof(TestController).Assembly.GetName().Name
        });
    }

    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.Now,
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId
        });
    }
} 