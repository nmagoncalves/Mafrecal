using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mafrecal.WorkerService.Services
{

    using Mafrecal.WorkerService.Data;
    using Mafrecal.WorkerService.Logging;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Mafrecal.WorkerService.Logging;

    public class StoresaceService
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;
        private readonly int _defaultLookbackDays;
        private readonly SqlService _sql;



        public StoresaceService(string baseUrl, string user, string password, string defaultLookbackDays, SqlService sql)
        {
            _baseUrl = baseUrl;
            _client = new HttpClient();
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _defaultLookbackDays = Convert.ToInt32(defaultLookbackDays);
            _sql = sql;
        }


        /// <summary>
        /// Obtém todos os itens de um endpoint de forma paginada, retornando JSON bruto (string) para performance.
        /// </summary>
 public async Task<List<string>> GetEndpointSalesAsync(string endpoint, int pageSize = 50, CancellationToken cancellationToken = default)
        {
            var allItems = new List<string>();
            int page = 1;
            //     List<string> stores = await _sql.GetStores();

            List<string> stores =new List<string>();
            //stores.Add("8");
            //stores.Add("22");
            stores.Add("24");
            var date = DateTime.Today.AddDays(-_defaultLookbackDays).ToString("yyyy-MM-dd");


            int ano = 2025;
            DateTime inicio = new DateTime(ano, 12, 1);
            DateTime fim = new DateTime(ano, 12, 31);

            for (DateTime dia = inicio; dia <= fim; dia = dia.AddDays(1))
            {
        
     
            foreach (var storeId in stores)
            {

               while (!cancellationToken.IsCancellationRequested)
                {
                    HttpResponseMessage response;
                    string url = $"{_baseUrl}{endpoint}/?format=json&page_size={pageSize}&page={page}&date={dia.ToString("yyyy-MM-dd")}&store={storeId}";

                    try
                    {
                    
                        response = await _client.GetAsync(url, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        Logger.Error($"Erro GetEndpointSalesAsync. Não foi possível processar o endereço {url}");
    
                        AppLogger.Error(
                            "Erro no endpoint",
                            ex: $"Erro GetEndpointSalesAsync. Não foi possível processar o endereço {url}",
                            endpoint: "GetEndpointSalesAsync"
                        );
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Erro GetEndpointSalesAsync. Não foi possível processar o endereço {url}");

                        AppLogger.Error(
                            "Erro no endpoint",
                            ex: $"Erro GetEndpointSalesAsync. Não foi possível processar o endereço.",
                            endpoint: "GetEndpointSalesAsync"
                        );

                        continue;
                    }

                    // TRATAMENTO DO 403
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        //Logger.Error($"Endereço {url} provavelemente sem dados para o dia {date} e loja {storeId}");
                        //AppLogger.Error(
                        //    "Erro no endpoint",
                        //    ex: $"Endereço {url} provavelemente sem dados para o dia {date} e loja {storeId}",
                        //    endpoint: "GetEndpointSalesAsync"
                        //);
                        continue; // passa para a próxima store
                    }

                    // Outros erros HTTP
                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Error($"Endereço {url} com erro desconhecido.");
                        AppLogger.Error(
                            "Erro no endpoint",
                            ex: $"Endereço {url} com erro desconhecido.",
                            endpoint: "GetEndpointSalesAsync"
                        );
                        continue;
                    }

                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                    var items = doc.RootElement.GetProperty("results").EnumerateArray();

                    int count = 0;
                    foreach (var item in items)
                    {
                        allItems.Add(item.GetRawText()); // Armazena JSON bruto
                        count++;
                    }

                    if (count < pageSize)
                        break; // última página
                    page++;
                }
            }
            }

            return allItems;
        }


   public async Task<List<string>> GetEndpointPurchasesAsync(
    string endpoint,
    int pageSize = 50,
    CancellationToken cancellationToken = default)
        {
            var allItems = new List<string>();
            int page = 1;

            while (!cancellationToken.IsCancellationRequested)
            {
                string synccounter = "-1";
              
                long lastCounter = await _sql.LastSyncCounter(endpoint.Replace("/", ""), cancellationToken);
                if (lastCounter!=0)
                {
                    synccounter = Convert.ToString(lastCounter);
                }
 
                var date = DateTime.Today.AddDays(-_defaultLookbackDays).ToString("yyyy-MM-dd");
                string url = $"{_baseUrl}{endpoint}/?format=json&page_size={pageSize}&page={page}&synccounter={synccounter}";
                HttpResponseMessage response;
                try
                {

                    response = await _client.GetAsync(url, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    Logger.Error($"Erro GetEndpointPurchasesAsync. Não foi possível processar o endereço {url}");

                    AppLogger.Error(
                        "Erro no endpoint",
                        ex: $"Erro GetEndpointPurchasesAsync. Não foi possível processar o endereço {url}",
                        endpoint: "GetEndpointPurchasesAsync"
                    );
                    continue;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Erro GetEndpointPurchasesAsync. Não foi possível processar o endereço {url}");

                    AppLogger.Error(
                        "Erro no endpoint",
                        ex: $"Erro GetEndpointPurchasesAsync. Não foi possível processar o endereço.",
                        endpoint: "GetEndpointPurchasesAsync"
                    );

                    continue;
                }

                // TRATAMENTO DO 403
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Logger.Error($"Endereço {url} provavelemente sem dados para o dia {date}");
                    AppLogger.Error(
                        "Erro no endpoint",
                        ex: $"Endereço {url} provavelemente sem dados para o dia {date}",

                        endpoint: "GetEndpointPurchasesAsync"
                    );
                    continue; // passa para a próxima store
                }

                // Outros erros HTTP
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Endereço {url} com erro desconhecido.");
                    AppLogger.Error(
                        "Erro no endpoint",
                        ex: $"Endereço {url} com erro desconhecido.",
                        endpoint: "GetEndpointSalesAsync"
                    );
                    continue;
                }


                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                var root = doc.RootElement;

                if (!root.TryGetProperty("results", out var results))
                    break;

                int added = 0;

                foreach (var item in results.EnumerateArray())
                {
                    allItems.Add(item.GetRawText());
                    added++;
                }

               
                if (added == 0)
                    break;

                if (root.TryGetProperty("next", out var next) &&
                    next.ValueKind == JsonValueKind.Null)
                {
                    break;
                }

                page++;
            }

            return allItems;
        }


        #region ENTIDADES
        public async Task<JsonElement?> GetSupplierByIdAsync(dynamic supplierId, CancellationToken ct)
        {
            string url = $"{_baseUrl}suppliers/?SupplierId={supplierId}";
        

            var response = await _client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null ;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            return Helpers.JsonHelper.ExtractFirstResult(doc);
        }
        public async Task<JsonElement?> GetItemFullByIdAsync(dynamic itemId, CancellationToken ct)
        {
            string url = $"{_baseUrl}items/?id={itemId}";


            var response = await _client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            return Helpers.JsonHelper.ExtractFirstResult(doc);
        }

        public async Task<JsonElement?> GetCustomerByIdAsync(dynamic customerId, CancellationToken ct)
        {
            string url = $"{_baseUrl}customers/?format=json&FederalTaxId={customerId}";


            var response = await _client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            return Helpers.JsonHelper.ExtractFirstResult(doc);
        }


        #endregion


    }


}