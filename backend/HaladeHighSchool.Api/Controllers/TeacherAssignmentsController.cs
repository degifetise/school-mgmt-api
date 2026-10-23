using HaladeHighSchool.Api.Configuration;
using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaladeHighSchool.Api.Controllers;

[ApiController]
[Route("api/admin/teachers/{teacherId:int}/assignments")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class TeacherAssignmentsController(ITeacherAssignmentService assignments) : PortalControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TeacherSectionAssignmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TeacherSectionAssignmentResponse>>> Get(
        int teacherId,
        CancellationToken cancellationToken)
    {
        var result = await assignments.GetSectionAssignmentsAsync(teacherId, CurrentSchoolId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<IReadOnlyList<TeacherSectionAssignmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TeacherSectionAssignmentResponse>>> Add(
        int teacherId,
        UpdateTeacherSectionAssignmentsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Assignments.Count == 0)
        {
            return BadRequestProblem("At least one grade and section is required.");
        }

        var result = await assignments.AddSectionAssignmentsAsync(
            teacherId,
            request.Assignments,
            CurrentSchoolId,
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut]
    [ProducesResponseType<IReadOnlyList<TeacherSectionAssignmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TeacherSectionAssignmentResponse>>> Replace(
        int teacherId,
        UpdateTeacherSectionAssignmentsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await assignments.ReplaceSectionAssignmentsAsync(
            teacherId,
            request.Assignments,
            CurrentSchoolId,
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{sectionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(
        int teacherId,
        int sectionId,
        CancellationToken cancellationToken)
    {
        var removed = await assignments.RemoveSectionAssignmentAsync(
            teacherId, sectionId, CurrentSchoolId, cancellationToken);
        return removed is null || !removed.Value ? NotFound() : NoContent();
    }

    private ActionResult<IReadOnlyList<TeacherSectionAssignmentResponse>> ToActionResult(
        TeacherSectionAssignmentsResult result)
    {
        if (!result.TeacherFound)
        {
            return NotFound();
        }

        return result.AssignmentsValid
            ? Ok(result.Assignments)
            : BadRequestProblem("One or more grade and section pairs are missing or inactive.");
    }

    private BadRequestObjectResult BadRequestProblem(string detail) =>
        BadRequest(new ProblemDetails
        {
            Title = "Invalid teacher assignments",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        });
}
