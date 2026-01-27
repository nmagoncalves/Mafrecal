namespace Mafrecal.WebDashboard.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string SourceEndpoint { get; set; }
        public string SourceEndpointId { get; set; }
        public string JsonData { get; set; }
        public bool Processed { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
    public class TransactionViewModel
    {
        public int Id { get; set; }
        public string SourceEndpoint { get; set; }
        public string SourceEndpointId { get; set; }
        public bool Processed { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public Dictionary<string, object> JsonData { get; set; }

        public string ReprocessStatus { get; set; }
    }


}
