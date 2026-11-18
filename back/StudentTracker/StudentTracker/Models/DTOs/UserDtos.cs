namespace StudentTracker.Models.DTOs;

public record UserProfileDto(
    Guid Id,
    string Username,
    string FullName,
    string Email
);
