using Microsoft.AspNetCore.Mvc;
using server.Models.Dtos;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/metrics/{id:int}")]
public class MetricDataController(MetricDataService service) : ControllerBase
{
    [HttpGet("fields")]
    public async Task<ActionResult<List<FieldFilterOptionDto>>> GetFields(int id)
    {
        var result = await service.GetFieldsAsync(id);
        return result is null
            ? NotFound(new { message = "לא הוגדר מבנה קובץ פעיל עבור מדד זה." })
            : Ok(result);
    }

    [HttpPost("data/query")]
    public async Task<ActionResult<PagedResultDto<MetricDataRowDto>>> QueryData(int id, [FromBody] MetricDataQueryDto query)
    {
        var (success, error, result) = await service.GetDataAsync(
            id, query.ImportBatchId, query.Year, query.Period, query.ValidOnly,
            query.Filters, query.SortField, query.SortDirection, query.Page, query.PageSize);

        if (!success) return BadRequest(new { message = error });
        return Ok(result);
    }
}
