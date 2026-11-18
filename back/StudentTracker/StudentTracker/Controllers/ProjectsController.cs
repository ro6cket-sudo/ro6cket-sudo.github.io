using Microsoft.AspNetCore.Mvc;
using StudentTracker.Models;
using StudentTracker.Models.DTOs;

namespace StudentTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private static readonly Guid SampleProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [HttpGet]
    public ActionResult<List<ProjectListItemDto>> GetMyProjects()
    {
        var sampleProjects = new List<ProjectListItemDto>
        {
            new(
                Id: SampleProjectId,
                Name: "StudentTracker",
                Description: "Система отслеживания багов в студенческих проектах",
                SoftwareVersion: "v1.2.0",
                GitRemoteUrl: "https://github.com/edu/student-tracker.git",
                CurrentUserRoles: new List<ProjectRole> { ProjectRole.Leader, ProjectRole.Developer },
                OpenTicketsCount: 7
            ),
            new(
                Id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name: "OnlineShop",
                Description: "Учебный интернет-магазин микросервисной архитектуры",
                SoftwareVersion: "v0.9.1-beta",
                GitRemoteUrl: "https://github.com/edu/online-shop.git",
                CurrentUserRoles: new List<ProjectRole> { ProjectRole.Tester },
                OpenTicketsCount: 3
            )
        };

        return Ok(sampleProjects);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<ProjectDetailsDto> GetProjectById(Guid id)
    {
        return Ok(new ProjectDetailsDto(
            Id: id,
            Name: "StudentTracker",
            Description: "Система отслеживания багов в студенческих проектах",
            SoftwareVersion: "v1.2.0",
            GitRemoteUrl: "https://github.com/edu/student-tracker.git",
            LeaderId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            LeaderName: "Дмитрий Л.",
            CreatedAt: DateTime.UtcNow.AddDays(-30),
            TotalTicketsCount: 18,
            MembersCount: 4
        ));
    }

    [HttpPost]
    public ActionResult<ProjectDetailsDto> CreateProject([FromBody] CreateProjectRequest request)
    {
        var newProject = new ProjectDetailsDto(
            Id: Guid.NewGuid(),
            Name: request.Name,
            Description: request.Description,
            SoftwareVersion: request.SoftwareVersion,
            GitRemoteUrl: request.GitRemoteUrl,
            LeaderId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            LeaderName: "Дмитрий Л.",
            CreatedAt: DateTime.UtcNow,
            TotalTicketsCount: 0,
            MembersCount: 1
        );

        return CreatedAtAction(nameof(GetProjectById), new { id = newProject.Id }, newProject);
    }


    [HttpPut("{id:guid}")]
    public ActionResult<ProjectDetailsDto> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    {
        return Ok(new ProjectDetailsDto(
            Id: id,
            Name: request.Name,
            Description: request.Description,
            SoftwareVersion: request.SoftwareVersion,
            GitRemoteUrl: request.GitRemoteUrl,
            LeaderId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            LeaderName: "Дмитрий Л.",
            CreatedAt: DateTime.UtcNow.AddDays(-30),
            TotalTicketsCount: 18,
            MembersCount: 4
        ));
    }

    [HttpDelete("{id:guid}")]
    public IActionResult DeleteProject(Guid id)
    {
        return NoContent();
    }
}
