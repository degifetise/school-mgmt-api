using HaladeHighSchool.Api.DTOs;

namespace HaladeHighSchool.Api.Services;

public interface IAnalyticsService
{
    Task<AdminAnalyticsOverviewResponse> GetAdminOverviewAsync(
        Guid schoolId,
        CancellationToken cancellationToken);

    Task<TeacherAnalyticsPerformanceResponse> GetTeacherPerformanceAsync(
        int? teacherId,
        Guid schoolId,
        CancellationToken cancellationToken);
}
