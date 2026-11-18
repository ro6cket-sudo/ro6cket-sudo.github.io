using Microsoft.AspNetCore.Mvc;
using StudentTracker.Models;
using StudentTracker.Models.DTOs;

namespace StudentTracker.Controllers;

[ApiController]
[Route("api")]
public class TicketsController : ControllerBase
{
    private static readonly Guid SampleTicketId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [HttpGet("projects/{projectId:guid}/tickets")]
    public ActionResult<List<TicketListItemDto>> GetProjectTickets(
        Guid projectId,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] Guid? assigneeId = null)
    {
        var tickets = new List<TicketListItemDto>
        {
            new(
                Id: SampleTicketId,
                TicketNumber: 101,
                Title: "Ошибка 500 при отправке пустого комментария",
                Status: TicketStatus.InProgress,
                Priority: TicketPriority.High,
                AssigneeId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                AssigneeName: "Алексей С.",
                Deadline: DateTime.UtcNow.AddDays(2),
                DiscoveryVersion: "v1.2.0",
                CreatedAt: DateTime.UtcNow.AddDays(-2)
            ),
            new(
                Id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                TicketNumber: 102,
                Title: "Неверный расчет скидки в корзине при промокоде",
                Status: TicketStatus.New,
                Priority: TicketPriority.Medium,
                AssigneeId: null,
                AssigneeName: null,
                Deadline: null,
                DiscoveryVersion: "v1.2.0",
                CreatedAt: DateTime.UtcNow.AddDays(-1)
            ),
            new(
                Id: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                TicketNumber: 103,
                Title: "Зависание интерфейса при загрузке тяжелых логов",
                Status: TicketStatus.OnReview,
                Priority: TicketPriority.Critical,
                AssigneeId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
                AssigneeName: "Елена М.",
                Deadline: DateTime.UtcNow.AddDays(1),
                DiscoveryVersion: "v1.1.9",
                CreatedAt: DateTime.UtcNow.AddDays(-4)
            ),
            new(
                Id: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                TicketNumber: 104,
                Title: "Опечатка в тексте подсказки на экране профиля",
                Status: TicketStatus.Closed,
                Priority: TicketPriority.Low,
                AssigneeId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                AssigneeName: "Алексей С.",
                Deadline: DateTime.UtcNow.AddDays(-3),
                DiscoveryVersion: "v1.1.8",
                CreatedAt: DateTime.UtcNow.AddDays(-7)
            )
        };

        if (status.HasValue)
        {
            tickets = tickets.Where(t => t.Status == status.Value).ToList();
        }

        if (priority.HasValue)
        {
            tickets = tickets.Where(t => t.Priority == priority.Value).ToList();
        }

        if (assigneeId.HasValue)
        {
            tickets = tickets.Where(t => t.AssigneeId == assigneeId.Value).ToList();
        }

        return Ok(tickets);
    }

    [HttpPost("projects/{projectId:guid}/tickets")]
    public ActionResult<TicketDetailsDto> CreateTicket(Guid projectId, [FromBody] CreateTicketRequest request)
    {
        var created = new TicketDetailsDto(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            TicketNumber: 105,
            Title: request.Title,
            ReproductionSteps: request.ReproductionSteps,
            Status: TicketStatus.New,
            Priority: request.Priority,
            DiscoveryVersion: request.DiscoveryVersion,
            AuthorId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            AuthorName: "Ольга Т.",
            AssigneeId: null,
            AssigneeName: null,
            Deadline: request.Deadline,
            Attachments: request.Attachments ?? new List<string>(),
            Comments: new List<TicketCommentDto>(),
            History: new List<TicketHistoryItemDto>
            {
                new(Guid.NewGuid(), "Тикет создан в статусе Новый", "Ольга Т.", DateTime.UtcNow)
            },
            CreatedAt: DateTime.UtcNow
        );

        return CreatedAtAction(nameof(GetTicketById), new { id = created.Id }, created);
    }


    [HttpGet("tickets/{id:guid}")]
    public ActionResult<TicketDetailsDto> GetTicketById(Guid id)
    {
        return Ok(new TicketDetailsDto(
            Id: id,
            ProjectId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            TicketNumber: 101,
            Title: "Ошибка 500 при отправке пустого комментария",
            ReproductionSteps: "1. Открыть форму комментариев.\n2. Оставить поле пустым и нажать «Отправить».\nФактический: Ошибка 500.\nОжидаемый: Валидация поля «Поле обязательно».",
            Status: TicketStatus.InProgress,
            Priority: TicketPriority.High,
            DiscoveryVersion: "v1.2.0",
            AuthorId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            AuthorName: "Ольга Т.",
            AssigneeId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            AssigneeName: "Алексей С.",
            Deadline: DateTime.UtcNow.AddDays(2),
            Attachments: new List<string> { "crash_stacktrace.log", "screenshot_error_500.png" },
            Comments: new List<TicketCommentDto>
            {
                new(
                    Id: Guid.NewGuid(),
                    AuthorId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    AuthorName: "Алексей С.",
                    Text: "Воспроизвел локально, проблема в отсутствии проверки на null в DTO.",
                    Attachments: new List<string>(),
                    CreatedAt: DateTime.UtcNow.AddHours(-5)
                )
            },
            History: new List<TicketHistoryItemDto>
            {
                new(Guid.NewGuid(), "Тикет создан в статусе Новый", "Ольга Т.", DateTime.UtcNow.AddDays(-2)),
                new(Guid.NewGuid(), "Назначен исполнитель Алексей С., статус изменен на В работе", "Дмитрий Л.", DateTime.UtcNow.AddDays(-1))
            },
            CreatedAt: DateTime.UtcNow.AddDays(-2)
        ));
    }


    [HttpPatch("tickets/{id:guid}/assign")]
    public IActionResult AssignTicket(Guid id, [FromBody] AssignTicketRequest request)
    {
        return Ok(new
        {
            Message = request.AssigneeId.HasValue 
                ? $"Исполнитель назначен: {request.AssigneeId}" 
                : "Исполнитель снят, тикет переведен в статус Новый"
        });
    }

    [HttpPost("tickets/{id:guid}/review")]
    public IActionResult SubmitReview(Guid id, [FromBody] SubmitReviewRequest request)
    {
        return Ok(new
        {
            Message = $"Результат ({request.Resolution}) отправлен на ревью тестировщику. Статус: На ревью.",
            Resolution = request.Resolution.ToString()
        });
    }

    [HttpPost("tickets/{id:guid}/verify")]
    public IActionResult VerifyTicket(Guid id, [FromBody] VerifyTicketRequest request)
    {
        if (request.Approved)
        {
            return Ok(new { Message = "Тикет успешно проверен и закрыт. Статус: Закрыт (D15 — неизменяем)." });
        }

        return Ok(new
        {
            Message = "Тикет возвращен на доработку исполнителю. Статус: В работе.",
            Comment = request.Comment
        });
    }


    [HttpPost("tickets/{id:guid}/comments")]
    public ActionResult<TicketCommentDto> AddComment(Guid id, [FromBody] AddCommentRequest request)
    {
        var comment = new TicketCommentDto(
            Id: Guid.NewGuid(),
            AuthorId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            AuthorName: "Дмитрий Л.",
            Text: request.Text,
            Attachments: request.Attachments ?? new List<string>(),
            CreatedAt: DateTime.UtcNow
        );

        return StatusCode(StatusCodes.Status201Created, comment);
    }
}
