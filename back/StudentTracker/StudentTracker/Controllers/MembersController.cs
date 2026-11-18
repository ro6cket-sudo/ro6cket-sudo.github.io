using Microsoft.AspNetCore.Mvc;
using StudentTracker.Models;
using StudentTracker.Models.DTOs;

namespace StudentTracker.Controllers;

[ApiController]
[Route("api")]
public class MembersController : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/members")]
    public ActionResult<List<ProjectMemberDto>> GetProjectMembers(Guid projectId)
    {
        var members = new List<ProjectMemberDto>
        {
            new(
                UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username: "dmitry_lead",
                FullName: "Дмитрий Л.",
                Email: "dmitry.lead@edu.ru",
                Roles: new List<ProjectRole> { ProjectRole.Leader, ProjectRole.Developer },
                ActiveTicketsInWorkCount: 1,
                JoinedAt: DateTime.UtcNow.AddDays(-30)
            ),
            new(
                UserId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Username: "alex_dev",
                FullName: "Алексей С.",
                Email: "alex.dev@edu.ru",
                Roles: new List<ProjectRole> { ProjectRole.Developer },
                ActiveTicketsInWorkCount: 2,
                JoinedAt: DateTime.UtcNow.AddDays(-25)
            ),
            new(
                UserId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Username: "tester_olga",
                FullName: "Ольга Т.",
                Email: "tester.olga@edu.ru",
                Roles: new List<ProjectRole> { ProjectRole.Tester },
                ActiveTicketsInWorkCount: 0,
                JoinedAt: DateTime.UtcNow.AddDays(-25)
            ),
            new(
                UserId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Username: "elena_dev",
                FullName: "Елена М.",
                Email: "elena.dev@edu.ru",
                Roles: new List<ProjectRole> { ProjectRole.Developer },
                ActiveTicketsInWorkCount: 1,
                JoinedAt: DateTime.UtcNow.AddDays(-20)
            )
        };

        return Ok(members);
    }

    [HttpPost("projects/{projectId:guid}/invites")]
    public ActionResult<ProjectInviteDto> InviteMember(Guid projectId, [FromBody] InviteMemberRequest request)
    {
        var invite = new ProjectInviteDto(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            ProjectName: "StudentTracker",
            InviterName: "Дмитрий Л.",
            Email: request.Email,
            OfferedRoles: request.Roles,
            ExpiresAt: DateTime.UtcNow.AddDays(7),
            Status: "Pending"
        );

        return StatusCode(StatusCodes.Status201Created, invite);
    }

    [HttpDelete("projects/{projectId:guid}/members/{userId:guid}")]
    public IActionResult RemoveMember(Guid projectId, Guid userId)
    {
        return NoContent();
    }

    [HttpGet("invites/my")]
    public ActionResult<List<ProjectInviteDto>> GetMyInvites()
    {
        var sampleInvites = new List<ProjectInviteDto>
        {
            new(
                Id: Guid.Parse("77777777-7777-7777-7777-777777777777"),
                ProjectId: Guid.Parse("88888888-8888-8888-8888-888888888888"),
                ProjectName: "ExamSystem",
                InviterName: "Иван Руководитель",
                Email: "dmitry.lead@edu.ru",
                OfferedRoles: new List<ProjectRole> { ProjectRole.Developer, ProjectRole.Tester },
                ExpiresAt: DateTime.UtcNow.AddDays(5),
                Status: "Pending"
            )
        };

        return Ok(sampleInvites);
    }

    [HttpPost("invites/{id:guid}/accept")]
    public IActionResult AcceptInvite(Guid id)
    {
        return Ok(new { Message = "Приглашение принято. Вы добавлены в проект." });
    }

    [HttpPost("invites/{id:guid}/decline")]
    public IActionResult DeclineInvite(Guid id)
    {
        return Ok(new { Message = "Приглашение отклонено." });
    }
}
