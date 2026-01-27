using Mafrecal.WebDashboard.Data;
using Mafrecal.WebDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mafrecal.WebDashboard.Controllers
{
    public class LogsController : Controller
    {
        private readonly DashboardDbContext _context;
        public LogsController(DashboardDbContext context)
        {
            _context = context;
        }

        // GET: Logs
        public async Task<IActionResult> Index(string? level, string? endpoint)
        {
            // Query inicial
            //IQueryable<Log> query = _context.Logs;

            //// Filtros opcionais
            //if (!string.IsNullOrWhiteSpace(level))
            //{
            //    query = query.Where(l => l.Level == level);
            //}

            //if (!string.IsNullOrWhiteSpace(endpoint))
            //{
            //    query = query.Where(l => l.Endpoint == endpoint);
            //}

            //// Ordenar pelo Timestamp mais recente
            //query = query.OrderByDescending(l => l.Timestamp);

            //var logs = await query.ToListAsync();

            //// Passar filtros para a view para manter os valores nos inputs
            //ViewBag.FilterLevel = level;
            //ViewBag.FilterEndpoint = endpoint;

            return View();
        }

        // GET: Logs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var log = await _context.Logs
                .FirstOrDefaultAsync(m => m.Id == id);

            if (log == null) return NotFound();

            return View(log);
        }

        [HttpPost]
        public async Task<IActionResult> Data(
    DataTableRequestModel request,
    string level,
    string endpoint)
        {
            var query = _context.Logs.AsQueryable();

            if (!string.IsNullOrEmpty(level))
                query = query.Where(x => x.Level == level);

            if (!string.IsNullOrEmpty(endpoint))
                query = query.Where(x => x.Endpoint == endpoint);

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new {
                    x.Id,
                    x.SourceId,
                    Timestamp = x.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    x.Level,
                    x.Source,
                    x.Endpoint,
                    x.Message
                })
                .ToListAsync();

            return Json(new
            {
                draw = request.Draw,
                recordsTotal = total,
                recordsFiltered = total,
                data
            });
        }

    }
}
