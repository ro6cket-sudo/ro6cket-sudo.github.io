using Microsoft.AspNetCore.Mvc;
using StudentTracker.Models.DTOs;
using StudentTracker.Services;

namespace StudentTracker.Controllers;

[ApiController]
[Route("api")]
public class MembersController(InMemoryTrackerStore store) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/members")]
    public ActionResult<List<ProjectMemberDto>> GetProjectMembers(Guid projectId)
    {
        var members = store.GetProjectMembers(projectId);
        return members is null ? NotFound() : Ok(members);
    }

    [HttpPost("projects/{projectId:guid}/invites")]
    public ActionResult<ProjectInviteDto> InviteMember(Guid projectId, [FromBody] InviteMemberRequest request)
    {
        var invite = store.InviteMember(projectId, request);
        return invite is null ? NotFound() : StatusCode(StatusCodes.Status201Created, invite);
    }

    [HttpDelete("projects/{projectId:guid}/members/{userId:guid}")]
    public IActionResult RemoveMember(Guid projectId, Guid userId)
    {
        return store.RemoveMember(projectId, userId) ? NoContent() : NotFound();
    }

    [HttpGet("invites/my")]
    public ActionResult<List<ProjectInviteDto>> GetMyInvites()
    {
        return Ok(store.GetMyInvites());
    }

    [HttpPost("invites/{id:guid}/accept")]
    public IActionResult AcceptInvite(Guid id)
    {
        return store.AcceptInvite(id)
            ? Ok(new { Message = "Приглашение принято. Вы добавлены в проект." })
            : NotFound();
    }

    [HttpPost("invites/{id:guid}/decline")]
    public IActionResult DeclineInvite(Guid id)
    {
        return store.DeclineInvite(id)
            ? Ok(new { Message = "Приглашение отклонено." })
            : NotFound();
    }
}
