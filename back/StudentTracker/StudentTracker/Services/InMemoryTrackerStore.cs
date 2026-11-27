using StudentTracker.Models;
using StudentTracker.Models.DTOs;

namespace StudentTracker.Services;

/// <summary>
/// Thread-safe in-memory data store for StudentTracker without an external database.
/// </summary>
public sealed class InMemoryTrackerStore
{
    private readonly object _sync = new();

    private readonly UserProfileDto _currentUser = new(
        Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Username: "dmitry_lead",
        FullName: "Дмитрий Л.",
        Email: "dmitry.lead@edu.ru"
    );

    private readonly Dictionary<Guid, ProjectRecord> _projects = new();
    private readonly Dictionary<Guid, InviteRecord> _invites = new();
    private readonly Dictionary<Guid, TicketRecord> _tickets = new();

    public InMemoryTrackerStore()
    {
        Seed();
    }

    public UserProfileDto GetCurrentUser() => _currentUser;

    public List<ProjectListItemDto> GetMyProjects()
    {
        lock (_sync)
        {
            return _projects.Values
                .Where(p => p.Members.Any(m => m.UserId == _currentUser.Id))
                .Select(p =>
                {
                    var currentMember = p.Members.First(m => m.UserId == _currentUser.Id);
                    var openCount = _tickets.Values.Count(t => t.ProjectId == p.Id && t.Status != TicketStatus.Closed);
                    return new ProjectListItemDto(
                        Id: p.Id,
                        Name: p.Name,
                        Description: p.Description,
                        SoftwareVersion: p.SoftwareVersion,
                        GitRemoteUrl: p.GitRemoteUrl,
                        CurrentUserRoles: currentMember.Roles.ToList(),
                        OpenTicketsCount: openCount
                    );
                })
                .ToList();
        }
    }

    public ProjectDetailsDto? GetProjectById(Guid id)
    {
        lock (_sync)
        {
            return _projects.TryGetValue(id, out var project) ? ToProjectDetails(project) : null;
        }
    }

    public ProjectDetailsDto CreateProject(CreateProjectRequest request)
    {
        lock (_sync)
        {
            var roles = request.CreatorRoles is { Count: > 0 }
                ? request.CreatorRoles.Distinct().ToList()
                : new List<ProjectRole> { ProjectRole.Leader };

            var project = new ProjectRecord
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                SoftwareVersion = request.SoftwareVersion,
                GitRemoteUrl = request.GitRemoteUrl,
                LeaderId = _currentUser.Id,
                LeaderName = _currentUser.FullName,
                CreatedAt = DateTime.UtcNow,
                Members = new List<MemberRecord>
                {
                    new()
                    {
                        UserId = _currentUser.Id,
                        Username = _currentUser.Username,
                        FullName = _currentUser.FullName,
                        Email = _currentUser.Email,
                        Roles = roles,
                        JoinedAt = DateTime.UtcNow
                    }
                }
            };

            _projects[project.Id] = project;
            return ToProjectDetails(project);
        }
    }

    public ProjectDetailsDto? UpdateProject(Guid id, UpdateProjectRequest request)
    {
        lock (_sync)
        {
            if (!_projects.TryGetValue(id, out var project))
            {
                return null;
            }

            project.Name = request.Name;
            project.Description = request.Description;
            project.SoftwareVersion = request.SoftwareVersion;
            project.GitRemoteUrl = request.GitRemoteUrl;

            return ToProjectDetails(project);
        }
    }

    public bool DeleteProject(Guid id)
    {
        lock (_sync)
        {
            if (!_projects.Remove(id))
            {
                return false;
            }

            var ticketIds = _tickets.Values.Where(t => t.ProjectId == id).Select(t => t.Id).ToList();
            foreach (var ticketId in ticketIds)
            {
                _tickets.Remove(ticketId);
            }

            return true;
        }
    }

    public List<ProjectMemberDto>? GetProjectMembers(Guid projectId)
    {
        lock (_sync)
        {
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return null;
            }

            return project.Members
                .Select(m => new ProjectMemberDto(
                    UserId: m.UserId,
                    Username: m.Username,
                    FullName: m.FullName,
                    Email: m.Email,
                    Roles: m.Roles.ToList(),
                    ActiveTicketsInWorkCount: _tickets.Values.Count(t =>
                        t.ProjectId == projectId &&
                        t.AssigneeId == m.UserId &&
                        t.Status == TicketStatus.InProgress),
                    JoinedAt: m.JoinedAt
                ))
                .ToList();
        }
    }

    public ProjectInviteDto? InviteMember(Guid projectId, InviteMemberRequest request)
    {
        lock (_sync)
        {
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return null;
            }

            var invite = new InviteRecord
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                ProjectName = project.Name,
                InviterName = _currentUser.FullName,
                Email = request.Email,
                OfferedRoles = request.Roles.Distinct().ToList(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Status = "Pending"
            };

            _invites[invite.Id] = invite;
            return ToInviteDto(invite);
        }
    }

    public bool RemoveMember(Guid projectId, Guid userId)
    {
        lock (_sync)
        {
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return false;
            }

            return project.Members.RemoveAll(m => m.UserId == userId) > 0;
        }
    }

    public List<ProjectInviteDto> GetMyInvites()
    {
        lock (_sync)
        {
            return _invites.Values
                .Where(i => i.Email.Equals(_currentUser.Email, StringComparison.OrdinalIgnoreCase) && i.Status == "Pending")
                .Select(ToInviteDto)
                .ToList();
        }
    }

    public bool AcceptInvite(Guid id)
    {
        lock (_sync)
        {
            if (!_invites.TryGetValue(id, out var invite) || invite.Status != "Pending")
            {
                return false;
            }

            invite.Status = "Accepted";

            if (_projects.TryGetValue(invite.ProjectId, out var project))
            {
                var existing = project.Members.FirstOrDefault(m => m.UserId == _currentUser.Id);
                if (existing is null)
                {
                    project.Members.Add(new MemberRecord
                    {
                        UserId = _currentUser.Id,
                        Username = _currentUser.Username,
                        FullName = _currentUser.FullName,
                        Email = _currentUser.Email,
                        Roles = invite.OfferedRoles.ToList(),
                        JoinedAt = DateTime.UtcNow
                    });
                }
            }

            return true;
        }
    }

    public bool DeclineInvite(Guid id)
    {
        lock (_sync)
        {
            if (!_invites.TryGetValue(id, out var invite) || invite.Status != "Pending")
            {
                return false;
            }

            invite.Status = "Declined";
            return true;
        }
    }

    public List<TicketListItemDto>? GetProjectTickets(
        Guid projectId,
        TicketStatus? status,
        TicketPriority? priority,
        Guid? assigneeId)
    {
        lock (_sync)
        {
            if (!_projects.ContainsKey(projectId))
            {
                return null;
            }

            var query = _tickets.Values.Where(t => t.ProjectId == projectId);

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (priority.HasValue)
            {
                query = query.Where(t => t.Priority == priority.Value);
            }

            if (assigneeId.HasValue)
            {
                query = query.Where(t => t.AssigneeId == assigneeId.Value);
            }

            return query
                .OrderByDescending(t => t.TicketNumber)
                .Select(t => new TicketListItemDto(
                    Id: t.Id,
                    TicketNumber: t.TicketNumber,
                    Title: t.Title,
                    Status: t.Status,
                    Priority: t.Priority,
                    AssigneeId: t.AssigneeId,
                    AssigneeName: t.AssigneeName,
                    Deadline: t.Deadline,
                    DiscoveryVersion: t.DiscoveryVersion,
                    CreatedAt: t.CreatedAt
                ))
                .ToList();
        }
    }

    public TicketDetailsDto? CreateTicket(Guid projectId, CreateTicketRequest request)
    {
        lock (_sync)
        {
            if (!_projects.ContainsKey(projectId))
            {
                return null;
            }

            var nextNumber = _tickets.Values
                .Where(t => t.ProjectId == projectId)
                .Select(t => t.TicketNumber)
                .DefaultIfEmpty(100)
                .Max() + 1;

            var ticket = new TicketRecord
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                TicketNumber = nextNumber,
                Title = request.Title,
                ReproductionSteps = request.ReproductionSteps,
                Status = TicketStatus.New,
                Priority = request.Priority,
                DiscoveryVersion = request.DiscoveryVersion,
                AuthorId = _currentUser.Id,
                AuthorName = _currentUser.FullName,
                Deadline = request.Deadline,
                Attachments = request.Attachments?.ToList() ?? new List<string>(),
                Comments = new List<TicketCommentDto>(),
                History = new List<TicketHistoryItemDto>
                {
                    new(Guid.NewGuid(), "Тикет создан в статусе Новый", _currentUser.FullName, DateTime.UtcNow)
                },
                CreatedAt = DateTime.UtcNow
            };

            _tickets[ticket.Id] = ticket;
            return ToTicketDetails(ticket);
        }
    }

    public TicketDetailsDto? GetTicketById(Guid id)
    {
        lock (_sync)
        {
            return _tickets.TryGetValue(id, out var ticket) ? ToTicketDetails(ticket) : null;
        }
    }

    public (bool Found, string Message) AssignTicket(Guid id, AssignTicketRequest request)
    {
        lock (_sync)
        {
            if (!_tickets.TryGetValue(id, out var ticket))
            {
                return (false, "Тикет не найден.");
            }

            if (request.AssigneeId.HasValue)
            {
                var project = _projects[ticket.ProjectId];
                var member = project.Members.FirstOrDefault(m => m.UserId == request.AssigneeId.Value);
                var assigneeName = member?.FullName ?? _currentUser.FullName;

                ticket.AssigneeId = request.AssigneeId.Value;
                ticket.AssigneeName = assigneeName;
                if (ticket.Status == TicketStatus.New)
                {
                    ticket.Status = TicketStatus.InProgress;
                }

                var reasonText = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : $" (причина: {request.Reason})";
                ticket.History.Add(new TicketHistoryItemDto(
                    Guid.NewGuid(),
                    $"Назначен исполнитель {assigneeName}{reasonText}, статус: В работе",
                    _currentUser.FullName,
                    DateTime.UtcNow
                ));

                return (true, $"Исполнитель назначен: {assigneeName}");
            }

            ticket.AssigneeId = null;
            ticket.AssigneeName = null;
            ticket.Status = TicketStatus.New;
            ticket.History.Add(new TicketHistoryItemDto(
                Guid.NewGuid(),
                "Исполнитель снят, тикет переведен в статус Новый",
                _currentUser.FullName,
                DateTime.UtcNow
            ));

            return (true, "Исполнитель снят, тикет переведен в статус Новый");
        }
    }

    public (bool Found, string Message) SubmitReview(Guid id, SubmitReviewRequest request)
    {
        lock (_sync)
        {
            if (!_tickets.TryGetValue(id, out var ticket))
            {
                return (false, "Тикет не найден.");
            }

            ticket.Status = TicketStatus.OnReview;
            ticket.History.Add(new TicketHistoryItemDto(
                Guid.NewGuid(),
                $"Отправлен на ревью ({request.Resolution})",
                _currentUser.FullName,
                DateTime.UtcNow
            ));

            return (true, $"Результат ({request.Resolution}) отправлен на ревью тестировщику. Статус: На ревью.");
        }
    }

    public (bool Found, string Message) VerifyTicket(Guid id, VerifyTicketRequest request)
    {
        lock (_sync)
        {
            if (!_tickets.TryGetValue(id, out var ticket))
            {
                return (false, "Тикет не найден.");
            }

            if (request.Approved)
            {
                ticket.Status = TicketStatus.Closed;
                ticket.History.Add(new TicketHistoryItemDto(
                    Guid.NewGuid(),
                    "Тикет проверен и закрыт",
                    _currentUser.FullName,
                    DateTime.UtcNow
                ));
                return (true, "Тикет успешно проверен и закрыт. Статус: Закрыт (D15 — неизменяем).");
            }

            ticket.Status = TicketStatus.InProgress;
            if (!string.IsNullOrWhiteSpace(request.Comment))
            {
                ticket.Comments.Add(new TicketCommentDto(
                    Id: Guid.NewGuid(),
                    AuthorId: _currentUser.Id,
                    AuthorName: _currentUser.FullName,
                    Text: $"[Возврат с ревью] {request.Comment}",
                    Attachments: new List<string>(),
                    CreatedAt: DateTime.UtcNow
                ));
            }

            ticket.History.Add(new TicketHistoryItemDto(
                Guid.NewGuid(),
                "Тикет возвращен на доработку. Статус: В работе",
                _currentUser.FullName,
                DateTime.UtcNow
            ));

            return (true, "Тикет возвращен на доработку исполнителю. Статус: В работе.");
        }
    }

    public TicketCommentDto? AddComment(Guid id, AddCommentRequest request)
    {
        lock (_sync)
        {
            if (!_tickets.TryGetValue(id, out var ticket))
            {
                return null;
            }

            var comment = new TicketCommentDto(
                Id: Guid.NewGuid(),
                AuthorId: _currentUser.Id,
                AuthorName: _currentUser.FullName,
                Text: request.Text,
                Attachments: request.Attachments?.ToList() ?? new List<string>(),
                CreatedAt: DateTime.UtcNow
            );

            ticket.Comments.Add(comment);
            return comment;
        }
    }

    private ProjectDetailsDto ToProjectDetails(ProjectRecord project)
    {
        return new ProjectDetailsDto(
            Id: project.Id,
            Name: project.Name,
            Description: project.Description,
            SoftwareVersion: project.SoftwareVersion,
            GitRemoteUrl: project.GitRemoteUrl,
            LeaderId: project.LeaderId,
            LeaderName: project.LeaderName,
            CreatedAt: project.CreatedAt,
            TotalTicketsCount: _tickets.Values.Count(t => t.ProjectId == project.Id),
            MembersCount: project.Members.Count
        );
    }

    private static ProjectInviteDto ToInviteDto(InviteRecord invite) => new(
        Id: invite.Id,
        ProjectId: invite.ProjectId,
        ProjectName: invite.ProjectName,
        InviterName: invite.InviterName,
        Email: invite.Email,
        OfferedRoles: invite.OfferedRoles.ToList(),
        ExpiresAt: invite.ExpiresAt,
        Status: invite.Status
    );

    private static TicketDetailsDto ToTicketDetails(TicketRecord t) => new(
        Id: t.Id,
        ProjectId: t.ProjectId,
        TicketNumber: t.TicketNumber,
        Title: t.Title,
        ReproductionSteps: t.ReproductionSteps,
        Status: t.Status,
        Priority: t.Priority,
        DiscoveryVersion: t.DiscoveryVersion,
        AuthorId: t.AuthorId,
        AuthorName: t.AuthorName,
        AssigneeId: t.AssigneeId,
        AssigneeName: t.AssigneeName,
        Deadline: t.Deadline,
        Attachments: t.Attachments.ToList(),
        Comments: t.Comments.ToList(),
        History: t.History.ToList(),
        CreatedAt: t.CreatedAt
    );

    private void Seed()
    {
        var dmitry = new MemberRecord
        {
            UserId = _currentUser.Id,
            Username = _currentUser.Username,
            FullName = _currentUser.FullName,
            Email = _currentUser.Email,
            Roles = new List<ProjectRole> { ProjectRole.Leader, ProjectRole.Developer },
            JoinedAt = DateTime.UtcNow.AddDays(-30)
        };
        var alex = new MemberRecord
        {
            UserId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Username = "alex_dev",
            FullName = "Алексей С.",
            Email = "alex.dev@edu.ru",
            Roles = new List<ProjectRole> { ProjectRole.Developer },
            JoinedAt = DateTime.UtcNow.AddDays(-25)
        };
        var olga = new MemberRecord
        {
            UserId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Username = "tester_olga",
            FullName = "Ольга Т.",
            Email = "tester.olga@edu.ru",
            Roles = new List<ProjectRole> { ProjectRole.Tester },
            JoinedAt = DateTime.UtcNow.AddDays(-25)
        };
        var elena = new MemberRecord
        {
            UserId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Username = "elena_dev",
            FullName = "Елена М.",
            Email = "elena.dev@edu.ru",
            Roles = new List<ProjectRole> { ProjectRole.Developer },
            JoinedAt = DateTime.UtcNow.AddDays(-20)
        };

        var mainProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        _projects[mainProjectId] = new ProjectRecord
        {
            Id = mainProjectId,
            Name = "StudentTracker",
            Description = "Система отслеживания багов в студенческих проектах",
            SoftwareVersion = "v1.2.0",
            GitRemoteUrl = "https://github.com/edu/student-tracker.git",
            LeaderId = dmitry.UserId,
            LeaderName = dmitry.FullName,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            Members = new List<MemberRecord> { dmitry, alex, olga, elena }
        };

        var shopProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        _projects[shopProjectId] = new ProjectRecord
        {
            Id = shopProjectId,
            Name = "OnlineShop",
            Description = "Учебный интернет-магазин микросервисной архитектуры",
            SoftwareVersion = "v0.9.1-beta",
            GitRemoteUrl = "https://github.com/edu/online-shop.git",
            LeaderId = alex.UserId,
            LeaderName = alex.FullName,
            CreatedAt = DateTime.UtcNow.AddDays(-18),
            Members = new List<MemberRecord>
            {
                new()
                {
                    UserId = dmitry.UserId,
                    Username = dmitry.Username,
                    FullName = dmitry.FullName,
                    Email = dmitry.Email,
                    Roles = new List<ProjectRole> { ProjectRole.Tester },
                    JoinedAt = DateTime.UtcNow.AddDays(-15)
                },
                alex
            }
        };

        var examProjectId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        _projects[examProjectId] = new ProjectRecord
        {
            Id = examProjectId,
            Name = "ExamSystem",
            Description = "Платформа проведения студенческих зачётов и тестов",
            SoftwareVersion = "v0.4.0",
            GitRemoteUrl = "https://github.com/edu/exam-system.git",
            LeaderId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            LeaderName = "Иван Руководитель",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            Members = new List<MemberRecord>
            {
                new()
                {
                    UserId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    Username = "ivan_lead",
                    FullName = "Иван Руководитель",
                    Email = "ivan.lead@edu.ru",
                    Roles = new List<ProjectRole> { ProjectRole.Leader },
                    JoinedAt = DateTime.UtcNow.AddDays(-10)
                }
            }
        };

        var inviteId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        _invites[inviteId] = new InviteRecord
        {
            Id = inviteId,
            ProjectId = examProjectId,
            ProjectName = "ExamSystem",
            InviterName = "Иван Руководитель",
            Email = _currentUser.Email,
            OfferedRoles = new List<ProjectRole> { ProjectRole.Developer, ProjectRole.Tester },
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            Status = "Pending"
        };

        var t101 = new TicketRecord
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ProjectId = mainProjectId,
            TicketNumber = 101,
            Title = "Ошибка 500 при отправке пустого комментария",
            ReproductionSteps = "1. Открыть форму комментариев.\n2. Оставить поле пустым и нажать «Отправить».\nФактический: Ошибка 500.\nОжидаемый: Валидация поля «Поле обязательно».",
            Status = TicketStatus.InProgress,
            Priority = TicketPriority.High,
            DiscoveryVersion = "v1.2.0",
            AuthorId = olga.UserId,
            AuthorName = olga.FullName,
            AssigneeId = alex.UserId,
            AssigneeName = alex.FullName,
            Deadline = DateTime.UtcNow.AddDays(2),
            Attachments = new List<string> { "crash_stacktrace.log", "screenshot_error_500.png" },
            Comments = new List<TicketCommentDto>
            {
                new(
                    Id: Guid.NewGuid(),
                    AuthorId: alex.UserId,
                    AuthorName: alex.FullName,
                    Text: "Воспроизвел локально, проблема в отсутствии проверки на null в DTO.",
                    Attachments: new List<string>(),
                    CreatedAt: DateTime.UtcNow.AddHours(-5)
                )
            },
            History = new List<TicketHistoryItemDto>
            {
                new(Guid.NewGuid(), "Тикет создан в статусе Новый", olga.FullName, DateTime.UtcNow.AddDays(-2)),
                new(Guid.NewGuid(), "Назначен исполнитель Алексей С., статус изменен на В работе", dmitry.FullName, DateTime.UtcNow.AddDays(-1))
            },
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var t102 = new TicketRecord
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ProjectId = mainProjectId,
            TicketNumber = 102,
            Title = "Неверный расчет скидки в корзине при промокоде",
            ReproductionSteps = "1. Добавить товар в корзину.\n2. Применить промокод STUDENT10.\nФактический: скидка применяется дважды.",
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium,
            DiscoveryVersion = "v1.2.0",
            AuthorId = olga.UserId,
            AuthorName = olga.FullName,
            AssigneeId = null,
            AssigneeName = null,
            Deadline = null,
            Attachments = new List<string>(),
            Comments = new List<TicketCommentDto>(),
            History = new List<TicketHistoryItemDto>
            {
                new(Guid.NewGuid(), "Тикет создан в статусе Новый", olga.FullName, DateTime.UtcNow.AddDays(-1))
            },
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var t103 = new TicketRecord
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            ProjectId = mainProjectId,
            TicketNumber = 103,
            Title = "Зависание интерфейса при загрузке тяжелых логов",
            ReproductionSteps = "1. Открыть тикет с логом > 5 МБ.\nФактический: вкладка браузера не отвечает 10 секунд.",
            Status = TicketStatus.OnReview,
            Priority = TicketPriority.Critical,
            DiscoveryVersion = "v1.1.9",
            AuthorId = olga.UserId,
            AuthorName = olga.FullName,
            AssigneeId = elena.UserId,
            AssigneeName = elena.FullName,
            Deadline = DateTime.UtcNow.AddDays(1),
            Attachments = new List<string> { "heavy_log_sample.txt" },
            Comments = new List<TicketCommentDto>(),
            History = new List<TicketHistoryItemDto>
            {
                new(Guid.NewGuid(), "Тикет создан в статусе Новый", olga.FullName, DateTime.UtcNow.AddDays(-4)),
                new(Guid.NewGuid(), "Отправлен на ревью (Fixed)", elena.FullName, DateTime.UtcNow.AddHours(-6))
            },
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };

        var t104 = new TicketRecord
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            ProjectId = mainProjectId,
            TicketNumber = 104,
            Title = "Опечатка в тексте подсказки на экране профиля",
            ReproductionSteps = "1. Открыть профиль пользователя.\n2. Посмотреть на подсказку под email.",
            Status = TicketStatus.Closed,
            Priority = TicketPriority.Low,
            DiscoveryVersion = "v1.1.8",
            AuthorId = olga.UserId,
            AuthorName = olga.FullName,
            AssigneeId = alex.UserId,
            AssigneeName = alex.FullName,
            Deadline = DateTime.UtcNow.AddDays(-3),
            Attachments = new List<string>(),
            Comments = new List<TicketCommentDto>(),
            History = new List<TicketHistoryItemDto>
            {
                new(Guid.NewGuid(), "Тикет создан в статусе Новый", olga.FullName, DateTime.UtcNow.AddDays(-7)),
                new(Guid.NewGuid(), "Тикет проверен и закрыт", olga.FullName, DateTime.UtcNow.AddDays(-3))
            },
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        var shopTicket = new TicketRecord
        {
            Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            ProjectId = shopProjectId,
            TicketNumber = 101,
            Title = "Таймаут сервиса оплаты при повторном запросе",
            ReproductionSteps = "1. Оформить заказ.\n2. Дважды нажать «Оплатить».",
            Status = TicketStatus.New,
            Priority = TicketPriority.High,
            DiscoveryVersion = "v0.9.1-beta",
            AuthorId = dmitry.UserId,
            AuthorName = dmitry.FullName,
            Attachments = new List<string>(),
            Comments = new List<TicketCommentDto>(),
            History = new List<TicketHistoryItemDto>
            {
                new(Guid.NewGuid(), "Тикет создан в статусе Новый", dmitry.FullName, DateTime.UtcNow.AddDays(-1))
            },
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        foreach (var ticket in new[] { t101, t102, t103, t104, shopTicket })
        {
            _tickets[ticket.Id] = ticket;
        }
    }

    private sealed class ProjectRecord
    {
        public required Guid Id { get; init; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required string SoftwareVersion { get; set; }
        public required string GitRemoteUrl { get; set; }
        public required Guid LeaderId { get; init; }
        public required string LeaderName { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required List<MemberRecord> Members { get; init; }
    }

    private sealed class MemberRecord
    {
        public required Guid UserId { get; init; }
        public required string Username { get; init; }
        public required string FullName { get; init; }
        public required string Email { get; init; }
        public required List<ProjectRole> Roles { get; init; }
        public required DateTime JoinedAt { get; init; }
    }

    private sealed class InviteRecord
    {
        public required Guid Id { get; init; }
        public required Guid ProjectId { get; init; }
        public required string ProjectName { get; init; }
        public required string InviterName { get; init; }
        public required string Email { get; init; }
        public required List<ProjectRole> OfferedRoles { get; init; }
        public required DateTime ExpiresAt { get; init; }
        public required string Status { get; set; }
    }

    private sealed class TicketRecord
    {
        public required Guid Id { get; init; }
        public required Guid ProjectId { get; init; }
        public required int TicketNumber { get; init; }
        public required string Title { get; set; }
        public required string ReproductionSteps { get; set; }
        public required TicketStatus Status { get; set; }
        public required TicketPriority Priority { get; set; }
        public required string DiscoveryVersion { get; set; }
        public required Guid AuthorId { get; init; }
        public required string AuthorName { get; init; }
        public Guid? AssigneeId { get; set; }
        public string? AssigneeName { get; set; }
        public DateTime? Deadline { get; set; }
        public required List<string> Attachments { get; init; }
        public required List<TicketCommentDto> Comments { get; init; }
        public required List<TicketHistoryItemDto> History { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
