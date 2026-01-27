using Mafrecal.WebDashboard.Data;
using Mafrecal.WebDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mafrecal.WebDashboard.Controllers
{


    [ApiController]
    [Route("Reprocess")]
    [IgnoreAntiforgeryToken]
    public class ReprocessController : ControllerBase
    {
        private readonly DashboardDbContext _context;

        public ReprocessController(DashboardDbContext context)
        {
            _context = context;
        }

        [HttpPost("Request")]
        public async Task<IActionResult> Request([FromBody] ReprocessRequestDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.SourceEndpoint) ||
                string.IsNullOrWhiteSpace(dto.SourceEndpointId))
            {
                return BadRequest("Pedido inválido.");
            }

            bool exists = await _context.ReprocessRequests.AnyAsync(r =>
                r.SourceEndpoint == dto.SourceEndpoint &&
                r.SourceEndpointId == dto.SourceEndpointId &&
                (r.Status == "Pending" || r.Status == "Running"));

            if (exists)
                return Conflict(new { message = "Já existe um pedido em execução." });

            var request = new ReprocessRequest
            {
                SourceEndpoint = dto.SourceEndpoint,
                SourceEndpointId = dto.SourceEndpointId,
                RequestedAt = DateTime.UtcNow,
                Status = "Pending",
                Processed = false
            };

            _context.ReprocessRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Pedido de reprocessamento registado com sucesso."
            });
        }
    }
}

