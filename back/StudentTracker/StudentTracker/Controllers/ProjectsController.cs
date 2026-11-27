using Microsoft.AspNetCore.Mvc;
using StudentTracker.Models.DTOs;
using StudentTracker.Services;

namespace StudentTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController(InMemoryTrackerStore store) : ControllerBase
{
    [HttpGet]
    public ActionResult<List<ProjectListItemDto>> GetMyProjects()
    {
        return Ok(store.GetMyProjects());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<ProjectDetailsDto> GetProjectById(Guid id)
    {
        var project = store.GetProjectById(id);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public ActionResult<ProjectDetailsDto> CreateProject([FromBody] CreateProjectRequest request)
    {
        var newProject = store.CreateProject(request);
        return CreatedAtAction(nameof(GetProjectById), new { id = newProject.Id }, newProject);
    }

    [HttpPut("{id:guid}")]
    public ActionResult<ProjectDetailsDto> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    {
        var updated = store.UpdateProject(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public IActionResult DeleteProject(Guid id)
    {
        return store.DeleteProject(id) ? NoContent() : NotFound();
    }
}
