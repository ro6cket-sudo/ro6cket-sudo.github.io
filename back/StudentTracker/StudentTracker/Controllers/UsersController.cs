using Microsoft.AspNetCore.Mvc;
using StudentTracker.Models.DTOs;

namespace StudentTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<UserProfileDto> GetCurrentUser()
    {
        return Ok(new UserProfileDto(
            Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Username: "dmitry_lead",
            FullName: "Дмитрий Л.",
            Email: "dmitry.lead@edu.ru"
        ));
    }
}
