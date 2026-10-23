using HaladeHighSchool.Api.DTOs;

namespace HaladeHighSchool.Api.Services;

public interface IEventService
{
    Task<PaginatedResult<EventDto>> GetEventsAsync(int page, int pageSize, string? search, string? category, string? status, CancellationToken cancellationToken = default, string? targetStakeholders = null, int? gradeLevelId = null, int? sectionId = null, bool includeAllTargets = false, Guid? schoolId = null);
    Task<EventDto?> GetEventAsync(int id, Guid? schoolId = null, CancellationToken cancellationToken = default);
    Task<EventDto> CreateEventAsync(CreateEventDto request, string? organizerUserId, Guid schoolId, CancellationToken cancellationToken = default);
    Task<EventDto?> UpdateEventAsync(int id, UpdateEventDto request, Guid schoolId, CancellationToken cancellationToken = default);
    Task<EventDto?> UpdateStatusAsync(int id, string status, Guid schoolId, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAsync(int id, Guid schoolId, CancellationToken cancellationToken = default);
    Task<EventRegistrationDto> RegisterAsync(int eventId, int studentId, Guid schoolId, CancellationToken cancellationToken = default);
    Task<bool> CancelRegistrationAsync(int eventId, int studentId, Guid schoolId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventRegistrationDto>> GetRegistrationsAsync(int eventId, Guid schoolId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventDto>> GetUpcomingEventsAsync(int count, Guid? schoolId = null, CancellationToken cancellationToken = default);
    Task<int> GetTotalRegistrationsCountAsync(Guid? schoolId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventDto>> GetPopularEventsAsync(int top, Guid? schoolId = null, CancellationToken cancellationToken = default);
}

public sealed class EventServiceException : Exception
{
    public EventServiceException(string message, int statusCode, Exception? innerException = null)
        : base(message, innerException) => StatusCode = statusCode;
    public int StatusCode { get; }
}