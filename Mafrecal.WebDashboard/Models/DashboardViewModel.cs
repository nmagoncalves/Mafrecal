namespace Mafrecal.WebDashboard.Models
{
    public class DashboardViewModel
    {
        public Dictionary<string, int> TotalPorEndpoint { get; set; }
        public Dictionary<string, int> ProcessadosPorEndpoint { get; set; }
        public Dictionary<string, int> ErrosPorEndpoint { get; set; }

        public int TotalRegistos { get; set; }
        public int TotalProcessados { get; set; }
        public int TotalPendentes { get; set; }
        public int TotalErros { get; set; }

        public List<Transaction> UltimosErros { get; set; }
    }

}
