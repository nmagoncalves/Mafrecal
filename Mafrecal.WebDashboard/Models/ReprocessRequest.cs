namespace Mafrecal.WebDashboard.Models
{
    public class ReprocessRequest
    {
        public int Id { get; set; }

        public string SourceEndpoint { get; set; }
        public string SourceEndpointId { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public bool Processed { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public string Error { get; set; }

        public string Status { get; set; }
    }
}
