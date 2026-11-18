namespace StudentTracker.Models.DTOs;

public record ProjectListItemDto(
    Guid Id,
    string Name,
    string? Description,
    string SoftwareVersion,
    string GitRemoteUrl,
    List<ProjectRole> CurrentUserRoles,
    int OpenTicketsCount
);

public record ProjectDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    string SoftwareVersion,
    string GitRemoteUrl,
    Guid LeaderId,
    string LeaderName,
    DateTime CreatedAt,
    int TotalTicketsCount,
    int MembersCount
);

public record CreateProjectRequest(
    string Name,
    string? Description,
    string SoftwareVersion,
    string GitRemoteUrl,
    List<ProjectRole> CreatorRoles
);

public record UpdateProjectRequest(
    string Name,
    string? Description,
    string SoftwareVersion,
    string GitRemoteUrl
);
