using HaladeHighSchool.Api.Configuration;
using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace HaladeHighSchool.Api.Controllers;

[ApiController]
[Route("api/report-cards")]
[Produces("application/pdf")]
public class ReportCardsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPdfReportCardService _pdfReportCards;

    public ReportCardsController(ApplicationDbContext db, IPdfReportCardService pdfReportCards)
    {
        _db = db;
        _pdfReportCards = pdfReportCards;
    }

    [HttpGet("students/{studentId:int}")]
    [Authorize(Roles = Roles.AdminOrTeacher + "," + Roles.Guardian + "," + Roles.Student)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadStudentPdf(int studentId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(Roles.Student))
        {
            var studentIdFromToken = User.GetStudentId();
            if (studentIdFromToken is null || studentIdFromToken.Value != studentId)
            {
                return Forbid();
            }
        }

        if (User.IsInRole(Roles.Guardian))
        {
            var guardianId = User.GetGuardianId();
            if (guardianId is null)
            {
                return Forbid();
            }

            var isLinked = await _db.StudentGuardians
                .AsNoTracking()
                .AnyAsync(sg => sg.GuardianId == guardianId.Value && sg.StudentId == studentId, cancellationToken);

            if (!isLinked)
            {
                return Forbid();
            }

            var linked = await _pdfReportCards.GenerateAsync(studentId, cancellationToken);
            if (linked is null)
            {
                return NotFound();
            }

            return File(linked, "application/pdf", $"report-card-{studentId}.pdf");
        }

        if (User.IsInRole(Roles.Student))
        {
            var studentPdf = await _pdfReportCards.GenerateAsync(studentId, cancellationToken);
            if (studentPdf is null)
            {
                return NotFound();
            }

            return File(studentPdf, "application/pdf", $"report-card-{studentId}.pdf");
        }

        if (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Teacher))
        {
            if (User.IsInRole(Roles.Teacher) && !User.IsInRole(Roles.Admin))
            {
                var teacherId = User.GetTeacherId();
                if (teacherId is null)
                {
                    return Forbid();
                }

                var isAssigned = await _db.TeacherSectionAssignments
                    .AsNoTracking()
                    .AnyAsync(assignment =>
                        assignment.TeacherId == teacherId.Value
                        && _db.Students.Any(student =>
                            student.Id == studentId
                            && student.GradeLevelId == assignment.GradeLevelId
                            && student.SectionId == assignment.SectionId),
                        cancellationToken);

                if (!isAssigned)
                {
                    return Forbid();
                }
            }

            var pdf = await _pdfReportCards.GenerateAsync(studentId, cancellationToken);
            if (pdf is null)
            {
                return NotFound();
            }

            return File(pdf, "application/pdf", $"report-card-{studentId}.pdf");
        }

        return Forbid();
    }
}
