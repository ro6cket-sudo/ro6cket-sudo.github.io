namespace StudentTracker.Models.DTOs;

public record ProjectMemberDto(
    Guid UserId,
    string Username,
    string FullName,
    string Email,
    List<ProjectRole> Roles,
    int ActiveTicketsInWorkCount,
    DateTime JoinedAt
);

public record InviteMemberRequest(
    string Email,
    List<ProjectRole> Roles
);

public record ProjectInviteDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string InviterName,
    string Email,
    List<ProjectRole> OfferedRoles,
    DateTime ExpiresAt,
    string Status
);
