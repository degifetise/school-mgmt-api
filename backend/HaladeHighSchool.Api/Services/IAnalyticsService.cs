using HaladeHighSchool.Api.DTOs;

namespace HaladeHighSchool.Api.Services;

public interface IAnalyticsService
{
    Task<AdminAnalyticsOverviewResponse> GetAdminOverviewAsync(CancellationToken cancellationToken);

    Task<TeacherAnalyticsPerformanceResponse> GetTeacherPerformanceAsync(
        int? teacherId,
        CancellationToken cancellationToken);
}
