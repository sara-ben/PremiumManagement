using Microsoft.AspNetCore.Mvc;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/imports")]
public class ImportsController(FileImportService service) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Import(
        [FromForm] int metricId,
        [FromForm] int year,
        [FromForm] string period,
        [FromForm] IFormFile file)
    {
        if (file.Length == 0) return BadRequest(new { message = "לא הועלה קובץ." });

        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "סוג קובץ לא נתמך. יש להעלות קובץ Excel (.xlsx/.xls)." });
        }

        await using var stream = file.OpenReadStream();
        var (success, error, result) = await service.ImportAsync(metricId, year, period, stream, file.FileName);

        if (!success) return BadRequest(new { message = error });
        return Ok(result);
    }
}
