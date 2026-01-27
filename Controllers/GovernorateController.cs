using E_Commerce.Dtos.GovernorateDto;
using E_Commerce.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GovernorateController : ControllerBase
    {
        private readonly IUnitOfWork work;

        public GovernorateController(IUnitOfWork unitOfWork)
        {
            work = unitOfWork;
        }

        // GET: /api/governorate
        [HttpGet]
        public async Task<IActionResult> GetAllGovernorates()
        {
            try
            {
                var governorates = await work.Governorates.GetAllGovernoratesAsync();

                var result = governorates.Select(g => new GovernorateDto
                {
                    Id = g.Id,
                    NameAr = g.NameAr,
                    NameEn = g.NameEn,
                    ShippingCost = g.ShippingCost
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving governorates", error = ex.Message });
            }
        }

        // GET: /api/governorate/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetGovernorateById(int id)
        {
            try
            {
                var governorate = await work.Governorates.GetGovernorateByIdAsync(id);

                if (governorate == null)
                    return NotFound(new { message = "Governorate not found" });

                var result = new GovernorateDto
                {
                    Id = governorate.Id,
                    NameAr = governorate.NameAr,
                    NameEn = governorate.NameEn,
                    ShippingCost = governorate.ShippingCost
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving governorate", error = ex.Message });
            }
        }

        // PUT: /api/governorate/{id}/shipping
        [HttpPut("{id:int}/shipping")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> UpdateShippingCost(int id, [FromBody] UpdateShippingCostDto dto)
        {
            try
            {
                if (dto.ShippingCost < 0)
                    return BadRequest(new { message = "Shipping cost cannot be negative" });

                var updated = await work.Governorates.UpdateShippingCostAsync(id, dto.ShippingCost);

                if (!updated)
                    return NotFound(new { message = "Governorate not found" });

                return Ok(new { message = "Shipping cost updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating shipping cost", error = ex.Message });
            }
        }
    }
}
