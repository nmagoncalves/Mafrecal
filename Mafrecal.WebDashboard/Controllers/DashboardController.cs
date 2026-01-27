using Mafrecal.WebDashboard.Data;
using Mafrecal.WebDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Mafrecal.WebDashboard.Controllers
{
   
    public class DashboardController : Controller
    {
        private readonly DashboardDbContext _context;

        public DashboardController(DashboardDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            // Totais globais
            model.TotalRegistos = await _context.Transactions.CountAsync();
            model.TotalProcessados = await _context.Transactions.CountAsync(t => t.Processed == true);
            model.TotalPendentes = await _context.Transactions.CountAsync(t => t.Processed != true);
            model.TotalErros = await _context.Transactions.CountAsync(t => t.Error != null && t.Error != "");

            // Agrupamentos por endpoint
            model.TotalPorEndpoint = await _context.Transactions
                .GroupBy(t => t.SourceEndpoint)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            model.ProcessadosPorEndpoint = await _context.Transactions
                .Where(t => t.Processed == true)
                .GroupBy(t => t.SourceEndpoint)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            model.ErrosPorEndpoint = await _context.Transactions
                .Where(t => t.Error != null && t.Error != "")
                .GroupBy(t => t.SourceEndpoint)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // Últimos 10 erros
            model.UltimosErros = await _context.Transactions
                .Where(t => t.Error != null && t.Error != "")
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

            return View(model);
        }
    }

}
