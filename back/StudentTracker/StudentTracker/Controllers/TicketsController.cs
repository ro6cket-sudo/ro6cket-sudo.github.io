using Microsoft.AspNetCore.Mvc;
using StudentTracker.Models;
using StudentTracker.Models.DTOs;
using StudentTracker.Services;

namespace StudentTracker.Controllers;

[ApiController]
[Route("api")]
public class TicketsController(InMemoryTrackerStore store) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/tickets")]
    public ActionResult<List<TicketListItemDto>> GetProjectTickets(
        Guid projectId,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] Guid? assigneeId = null)
    {
        var tickets = store.GetProjectTickets(projectId, status, priority, assigneeId);
        return tickets is null ? NotFound() : Ok(tickets);
    }

    [HttpPost("projects/{projectId:guid}/tickets")]
    public ActionResult<TicketDetailsDto> CreateTicket(Guid projectId, [FromBody] CreateTicketRequest request)
    {
        var created = store.CreateTicket(projectId, request);
        return created is null
            ? NotFound()
            : CreatedAtAction(nameof(GetTicketById), new { id = created.Id }, created);
    }

    [HttpGet("tickets/{id:guid}")]
    public ActionResult<TicketDetailsDto> GetTicketById(Guid id)
    {
        var ticket = store.GetTicketById(id);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPatch("tickets/{id:guid}/assign")]
    public IActionResult AssignTicket(Guid id, [FromBody] AssignTicketRequest request)
    {
        var (found, message) = store.AssignTicket(id, request);
        return found ? Ok(new { Message = message }) : NotFound();
    }

    [HttpPost("tickets/{id:guid}/review")]
    public IActionResult SubmitReview(Guid id, [FromBody] SubmitReviewRequest request)
    {
        var (found, message) = store.SubmitReview(id, request);
        return found
            ? Ok(new { Message = message, Resolution = request.Resolution.ToString() })
            : NotFound();
    }

    [HttpPost("tickets/{id:guid}/verify")]
    public IActionResult VerifyTicket(Guid id, [FromBody] VerifyTicketRequest request)
    {
        var (found, message) = store.VerifyTicket(id, request);
        if (!found)
        {
            return NotFound();
        }

        return request.Approved
            ? Ok(new { Message = message })
            : Ok(new { Message = message, request.Comment });
    }

    [HttpPost("tickets/{id:guid}/comments")]
    public ActionResult<TicketCommentDto> AddComment(Guid id, [FromBody] AddCommentRequest request)
    {
        var comment = store.AddComment(id, request);
        return comment is null ? NotFound() : StatusCode(StatusCodes.Status201Created, comment);
    }
}
