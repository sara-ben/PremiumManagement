using Microsoft.AspNetCore.Mvc;
using server.Models.Dtos;
using server.Models.Enums;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/premium-methods")]
public class PremiumMethodsController(PremiumMethodService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<PremiumMethodListItemDto>>> GetList(
        [FromQuery] string? search,
        [FromQuery] CalculationPeriod? calculationPeriod,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(await service.GetListAsync(search, calculationPeriod, page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PremiumMethodDetailDto>> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PremiumMethodDetailDto>> Create([FromBody] PremiumMethodCreateDto dto)
    {
        var (success, error, result) = await service.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PremiumMethodUpdateDto dto)
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
}
