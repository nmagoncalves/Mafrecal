using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mafrecal.WorkerService.Data
{
    public class ReprocessRequest
    {
        public int Id { get; set; }
        public string SourceEndpoint { get; set; }
        public string SourceEndpointId { get; set; }
        public string JsonData { get; set; }   // vem da Transactions
    }

}
