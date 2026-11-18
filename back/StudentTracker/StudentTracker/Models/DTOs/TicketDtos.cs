namespace StudentTracker.Models.DTOs;

public record TicketListItemDto(
    Guid Id,
    int TicketNumber,
    string Title,
    TicketStatus Status,
    TicketPriority Priority,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTime? Deadline,
    string DiscoveryVersion,
    DateTime CreatedAt
);

public record TicketDetailsDto(
    Guid Id,
    Guid ProjectId,
    int TicketNumber,
    string Title,
    string ReproductionSteps,
    TicketStatus Status,
    TicketPriority Priority,
    string DiscoveryVersion,
    Guid AuthorId,
    string AuthorName,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTime? Deadline,
    List<string> Attachments,
    List<TicketCommentDto> Comments,
    List<TicketHistoryItemDto> History,
    DateTime CreatedAt
);

public record CreateTicketRequest(
    string Title,
    string ReproductionSteps,
    TicketPriority Priority,
    string DiscoveryVersion,
    DateTime? Deadline,
    List<string>? Attachments
);

public record AssignTicketRequest(
    Guid? AssigneeId,
    string? Reason
);

public record SubmitReviewRequest(
    ResolutionType Resolution,
    string? ReleaseVersion,
    string? MergeRequestUrl,
    Guid? OriginalTicketId,
    string? Justification
);

public record VerifyTicketRequest(
    bool Approved,
    string Comment
);

public record AddCommentRequest(
    string Text,
    List<string>? Attachments
);

public record TicketCommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Text,
    List<string> Attachments,
    DateTime CreatedAt
);

public record TicketHistoryItemDto(
    Guid Id,
    string EventDescription,
    string PerformedByName,
    DateTime Timestamp
);
