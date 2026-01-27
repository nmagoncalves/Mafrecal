using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mafrecal.WorkerService.Data
{
    public class LogEntry
    {
        public string Level { get; set; }
        public string Source { get; set; }
        public string  SourceId { get; set; }
        public string Method { get; set; }
        public string Endpoint { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
    }
}
