using System.Security.Claims;
using HaladeHighSchool.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Middleware;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AllowDuringRequiredPasswordChangeAttribute : Attribute
{
}

public sealed class RequiredPasswordChangeMiddleware
{
    private readonly RequestDelegate _next;

    public RequiredPasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        var endpoint = context.GetEndpoint();
        if (context.User.Identity?.IsAuthenticated != true
            || endpoint is null
            || endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
            || endpoint.Metadata.GetMetadata<AllowDuringRequiredPasswordChangeAttribute>() is not null
            || endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count == 0)
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)
            || !await db.Users.AsNoTracking().AnyAsync(
                user => user.Id == userId && user.MustChangePasswordOnLogin,
                context.RequestAborted))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Type = "https://httpstatuses.com/403",
                Title = "Password change required",
                Status = StatusCodes.Status403Forbidden,
                Detail = "Change your temporary password before using the portal.",
                Instance = context.Request.Path,
            },
            context.RequestAborted);
    }
}
