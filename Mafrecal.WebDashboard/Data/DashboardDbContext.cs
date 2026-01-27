using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Mafrecal.WebDashboard.Models;

namespace Mafrecal.WebDashboard.Data
{
    public class DashboardDbContext : DbContext
    {
        public DashboardDbContext(DbContextOptions<DashboardDbContext> options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Log> Logs { get; set; }

        public DbSet<ReprocessRequest> ReprocessRequests { get; set; }
    }
}
