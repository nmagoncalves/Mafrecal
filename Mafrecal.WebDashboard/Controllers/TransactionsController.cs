using Mafrecal.WebDashboard.Data;
using Mafrecal.WebDashboard.Helpers;
using Mafrecal.WebDashboard.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mafrecal.WebDashboard.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly DashboardDbContext _context;

        public TransactionsController(DashboardDbContext context)
        {
            _context = context;

        }

        public async Task<IActionResult> Index(string endpoint, bool? processed)
        {
            var query = _context.Transactions.AsQueryable();

            if (!string.IsNullOrEmpty(endpoint))
                query = query.Where(t => t.SourceEndpoint == endpoint);

            if (processed.HasValue)
                query = query.Where(t => t.Processed == processed.Value);

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();


            var viewModels = transactions.Select(tx => new TransactionViewModel
            {
                Id = tx.Id,
                SourceEndpoint = tx.SourceEndpoint,
                SourceEndpointId = tx.SourceEndpointId,
                Processed = tx.Processed,
                Error = tx.Error,
                CreatedAt = tx.CreatedAt,
                ProcessedAt = tx.ProcessedAt,
                JsonData = JsonHelper.DeserializeToDictionary(tx.JsonData)


            }).ToList();


            var reprocessStates = await _context.ReprocessRequests
    .GroupBy(r => new { r.SourceEndpoint, r.SourceEndpointId })
    .Select(g => g
        .OrderByDescending(x => x.RequestedAt)
        .First())
    .ToDictionaryAsync(
        k => $"{k.SourceEndpoint}|{k.SourceEndpointId}",
        v => v.Status
    );

            foreach (var vm in viewModels)
            {
                var key = $"{vm.SourceEndpoint}|{vm.SourceEndpointId}";
                if (reprocessStates.TryGetValue(key, out var status))
                {
                    vm.ReprocessStatus = status;
                }
            }


            ViewBag.Endpoint = endpoint;
            return View(viewModels);
        }


        public async Task<IActionResult> Details(int id)
        {
            var tx = await _context.Transactions.FindAsync(id);
            if (tx == null) return NotFound();

            var viewModel = new TransactionViewModel
            {
                Id = tx.Id,
                SourceEndpoint = tx.SourceEndpoint,
                SourceEndpointId = tx.SourceEndpointId,
                Processed = tx.Processed,
                Error = tx.Error,
                CreatedAt = tx.CreatedAt,
                ProcessedAt = tx.ProcessedAt,
                JsonData = JsonHelper.DeserializeToDictionary(tx.JsonData)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Retry(int id)
        {
            var tx = await _context.Transactions.FindAsync(id);
            if (tx == null) return NotFound();

            try
            {
                string endpoint = tx.SourceEndpoint switch
                {
                    "purchases" => "Compras/Actualiza",
                    "sales" => "Vendas/Actualiza",
                    "stores" => "Lojas/Actualiza",
                    "articles" => "Artigos/Actualiza",
                    "clients" => "Clientes/Actualiza",
                    "suppliers" => "Fornecedores/Actualiza",
                    "interns" => "Interns/Actualiza",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(endpoint))
                {
     

                    tx.Processed = true;
                    tx.Error = null;
                    tx.ProcessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                tx.Error = ex.ToString();
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id });
        }

        private async Task<string> GetPrimaveraTokenAsync()
        {
            // Implementa chamada ao PrimaveraAuthService
            return await Task.FromResult("TOKEN_AQUI");
        }
    }

}
