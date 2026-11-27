using Microsoft.AspNetCore.Mvc;
using StudentTracker.Models.DTOs;
using StudentTracker.Services;

namespace StudentTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(InMemoryTrackerStore store) : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<UserProfileDto> GetCurrentUser()
    {
        return Ok(store.GetCurrentUser());
    }
}
