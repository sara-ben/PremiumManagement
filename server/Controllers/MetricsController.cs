using Microsoft.AspNetCore.Mvc;
using server.Models.Dtos;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/metrics")]
public class MetricsController(MetricService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MetricListItemDto>>> GetList([FromQuery] int? premiumMethodId)
    {
        return Ok(await service.GetListAsync(premiumMethodId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MetricDetailDto>> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<MetricDetailDto>> Create([FromBody] MetricCreateDto dto)
    {
        var (success, error, result) = await service.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] MetricUpdateDto dto)
    {
        var (success, error) = await service.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });
        return NoContent();
    }

    [HttpPatch("{id:int}/freeze")]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive = false)
    {
        var (success, error) = await service.SetActiveAsync(id, isActive);
        if (!success) return NotFound(new { message = error });
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await service.DeleteAsync(id);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPost("{id:int}/file-definitions")]
    public async Task<ActionResult<FileDefinitionDto>> CreateFileDefinition(int id, [FromBody] FileDefinitionCreateDto dto)
    {
        var (success, error, result) = await service.CreateFileDefinitionAsync(id, dto);
        if (!success) return BadRequest(new { message = error });
        return Ok(result);
    }

    [HttpPatch("{id:int}/file-definitions/{fileDefinitionId:int}/activate")]
    public async Task<IActionResult> ActivateFileDefinition(int id, int fileDefinitionId)
    {
        var (success, error) = await service.SetActiveFileDefinitionAsync(id, fileDefinitionId);
        if (!success) return NotFound(new { message = error });
        return NoContent();
    }
}
