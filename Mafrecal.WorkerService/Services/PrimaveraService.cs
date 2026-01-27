using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace Mafrecal.WorkerService.Services
{
    using Mafrecal.WorkerService.Helpers;
    using Mafrecal.WorkerService.Logging;
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class PrimaveraService
    {
        private readonly HttpClient _client;
        public PrimaveraService(string baseUrl) => _client = new HttpClient { BaseAddress = new System.Uri(baseUrl) };

        public void SetToken(string token) => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        public async Task<PrimaveraResponse> PostAsync(string endpoint, string json, int sourceId = 0)
        {
            try
            {


                if (_client == null)
                    throw new InvalidOperationException("_client não foi inicializado");

                var content = new StringContent(json ?? "", Encoding.UTF8, "application/json");
                var response = await _client.PostAsync(endpoint, content);

                var responseContent = await response.Content?.ReadAsStringAsync() ?? string.Empty;

                bool success = response.IsSuccessStatusCode;

                if (!success)
                {

                    Logger.Error($"Erro POST {endpoint}: {responseContent}");
                    AppLogger.Error(
                        "Erro",
                        ex: responseContent,
                        source: "Mafrecal.WorkerService",
                        endpoint: endpoint);
                }

                return new PrimaveraResponse
                {
                    Success = success,
                    ResponseContent = responseContent
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Exceção POST {endpoint}: {json}", ex);
                AppLogger.Error(
                    "Erro",
                    ex: ex?.ToString() ?? "Exceção desconhecida",
                    source: "Mafrecal.WorkerService",
                    endpoint: endpoint);

                return new PrimaveraResponse
                {
                    Success = false,
                    ResponseContent = ex?.ToString() ?? "Exceção desconhecida"
                };
            }
        }
    }

    //public class PrimaveraService
    //{
    //    private readonly HttpClient _client;
    //    private readonly PrimaveraTokenManager _tokenManager;

    //    public PrimaveraService(string baseUrl, PrimaveraTokenManager tokenManager)
    //    {
    //        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    //        _tokenManager = tokenManager;
    //    }

    //    private async Task EnsureTokenAsync()
    //    {
    //        var token = await _tokenManager.GetTokenAsync();
    //        _client.DefaultRequestHeaders.Authorization =
    //            new AuthenticationHeaderValue("Bearer", token);
    //    }

    //    public async Task<PrimaveraResponse> PostAsync(string endpoint, string json, int sourceId = 0)
    //    {
    //        await EnsureTokenAsync();

    //        var content = new StringContent(json, Encoding.UTF8, "application/json");
    //        var response = await _client.PostAsync(endpoint, content);
    //        var body = await response.Content.ReadAsStringAsync();

    //        return new PrimaveraResponse
    //        {
    //            Success = response.IsSuccessStatusCode,
    //            ResponseContent = body
    //        };
    //    }
    //}


    public class PrimaveraResponse
    {
        public bool Success { get; set; }
        public string ResponseContent { get; set; }
    }


}
