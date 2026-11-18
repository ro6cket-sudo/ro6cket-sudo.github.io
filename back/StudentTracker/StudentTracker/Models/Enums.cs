namespace StudentTracker.Models;

public enum ProjectRole
{
    Leader,
    Developer,
    Tester
}

public enum TicketStatus
{
    New,
    InProgress,
    OnReview,
    Closed
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum ResolutionType
{
    Fixed,
    NotABug,
    Duplicate,
    CannotReproduce
}
