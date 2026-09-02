using Microsoft.AspNetCore.Mvc;
using server.Models.Dtos;
using server.Models.Enums;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/import-history")]
public class ImportHistoryController(ImportHistoryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ImportBatchListItemDto>>> GetList(
        [FromQuery] int? metricId,
        [FromQuery] int? year,
        [FromQuery] string? period,
        [FromQuery] ImportStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(await service.GetListAsync(metricId, year, period, status, page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ImportResultDto>> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await service.DeleteAsync(id);
        if (!success) return NotFound(new { message = error });
        return NoContent();
    }
}
