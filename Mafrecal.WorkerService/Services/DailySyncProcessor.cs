using Mafrecal.WorkerService.Data;
using Mafrecal.WorkerService.Logging;
using Mafrecal.WorkerService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mafrecal.WorkerService.Services
{
    public class DailySyncProcessor
    {
        private readonly StoresaceService _storesace;
        private readonly PrimaveraService _primavera;
        private readonly SqlService _sql;

        public DailySyncProcessor(StoresaceService storesace, PrimaveraService primavera, SqlService sql)
        {
            _storesace = storesace;
            _primavera = primavera;
            _sql = sql;
        }

        /// <summary>
        /// Executa sincronização diária para todos os endpoints configurados
        /// </summary>
        public async Task RunDailySyncAsync(CancellationToken cancellationToken)
        {
            var endpoints = new Dictionary<string, string>
        {
          { "stores", "Lojas/Actualiza" }
            //    ,
           //  { "items", "Artigos/Actualiza" }
            //{ "clients", "Clientes/Actualiza" },
            //{ "suppliers", "Fornecedores/Actualiza" },
            //{ "interns", "Interns/Actualiza" }
        };

            foreach (var ep in endpoints)
            {
                try
                {
                   // await SyncEndpointAsync(ep.Key, ep.Value, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Erro no DailySync - endpoint {ep.Key}", ex);
                }
            }
        }

        //    private async Task SyncEndpointAsync(string sourceEndpoint, string primaveraEndpoint, CancellationToken cancellationToken)
        //    {
        //        // Obtém todos os itens do endpoint como JSON bruto
        //        var itemsJson = await _storesace.GetEndpointItemsAsync(sourceEndpoint, pageSize: 50, cancellationToken);

        //        foreach (var itemJson in itemsJson)
        //        {
        //            using var doc = JsonDocument.Parse(itemJson);
        //            var item = doc.RootElement;

        //            string id = item.GetProperty("Id").GetInt32().ToString();
        //            long syscounter = item.GetProperty("synccounter").GetInt64();

        //            // Evita duplicados
        //            if (await _sql.ExistsIntAsync(sourceEndpoint, Convert.ToString(id), syscounter, cancellationToken))
        //                continue;

        //            // Mapear para JSON Primavera
        //            string json = sourceEndpoint switch
        //            {
        //                "stores" => MapperService.MapStore(item),
        //                "items" => MapperService.MapArtigoGroup(item),
        //                "clients" => MapperService.MapCliente(item),
        //                "suppliers" => MapperService.MapFornecedor(item, item),

        //                _ => item.GetRawText()
        //            };

        //            // Envia para Primavera
        //            var result = await _primavera.PostAsync(primaveraEndpoint, json);

        //            // Marca processed ou error no SQL
        //            if (result.Success)
        //                await _sql.MarkAsProcessedAsync(sourceEndpoint, id, cancellationToken);
        //            else
        //                await _sql.MarkAsErrorAsync(sourceEndpoint, id, result.ResponseContent, cancellationToken);

        //            // Salva a transação na tabela de histórico
        //            await _sql.SaveTransactionAsync(sourceEndpoint, id, item.GetRawText(), syscounter, cancellationToken);

        //            Logger.Info($"[{sourceEndpoint}] Sincronizado item {id}");
        //        }
        //    }
        //}

    }
}


//public class DailySyncProcessor
//{
//    private readonly PrimaveraService _primavera;
//    private readonly SqlService _sql;
//    private readonly StoresaceService _storesace;

//    public DailySyncProcessor(
//        PrimaveraService primavera,
//        SqlService sql,
//        StoresaceService storesace)
//    {
//        _primavera = primavera;
//        _sql = sql;
//        _storesace = storesace;
//    }

//    /// <summary>
//    /// Executa o sync diário de todos endpoints
//    /// </summary>
//    public async Task RunDailySyncAsync(CancellationToken cancellationToken)
//    {
//        var endpointMappings = new Dictionary<string, string>
//        {
//            { "stores", "Lojas/Actualiza" },
//            { "articles", "Artigos/Actualiza" },
//            { "clients", "Clientes/Actualiza" },
//            { "suppliers", "Fornecedores/Actualiza" },
//            { "interns", "Interns/Actualiza" }
//        };

//        foreach (var kvp in endpointMappings)
//        {
//            string sourceEndpoint = kvp.Key;
//            string primaveraEndpoint = kvp.Value;

//            try
//            {
//                await SyncEndpointAsync(sourceEndpoint, primaveraEndpoint, cancellationToken);
//            }
//            catch (OperationCanceledException)
//            {
//                Logger.Info($"[DailySync] Cancelado pelo token no endpoint {sourceEndpoint}");
//                break;
//            }
//            catch (Exception ex)
//            {
//                Logger.Error($"[DailySync] Erro ao sincronizar {sourceEndpoint}", ex);
//            }
//        }
//    }

//    private async Task SyncEndpointAsync(string sourceEndpoint, string primaveraEndpoint, CancellationToken cancellationToken)
//    {
//        // Obtém os itens do endpoint
//        var items = await GetEndpointItemsAsync(sourceEndpoint, 50, cancellationToken);

//        foreach (var item in items)
//        {
//            cancellationToken.ThrowIfCancellationRequested();

//            int sourceId = item.GetProperty("Id").GetInt32();

//            // Verifica se já existe
//            bool exists = sourceEndpoint switch
//            {
//                "stores" => await _sql.ExistsStoreAsync(sourceId, cancellationToken),
//                "articles" => await _sql.ExistsArticleAsync(sourceId, cancellationToken),
//                "clients" => await _sql.ExistsAsync(sourceEndpoint, sourceId, cancellationToken),
//                "suppliers" => await _sql.ExistsAsync(sourceEndpoint, sourceId, cancellationToken),
//                "interns" => await _sql.ExistsAsync(sourceEndpoint, sourceId, cancellationToken),
//                _ => false
//            };

//            if (!exists)
//            {
//                string json = sourceEndpoint switch
//                {
//                    "stores" => MapperService.MapStore(item),
//                    "articles" => MapperService.MapArtigo(item),
//                    "clients" => MapperService.MapCliente(item),
//                    "suppliers" => MapperService.MapFornecedor(item),
//                    "interns" => MapperService.MapIntern(item),
//                    _ => item.GetRawText()
//                };

//                var result = await _primavera.PostAsync(primaveraEndpoint, json);

//                // Salva status no SQL
//                await _sql.SaveTransactionAsync(sourceEndpoint, sourceId, item.GetRawText(), cancellationToken);

//                if (result.Success)
//                    await _sql.MarkAsProcessedAsync(sourceEndpoint, sourceId, cancellationToken);
//                else
//                    await _sql.MarkAsErrorAsync(sourceEndpoint, sourceId, result.ResponseContent, cancellationToken);

//                Logger.Info($"[DailySync] {sourceEndpoint} id={sourceId} sincronizado com sucesso",
//                            source: "DailySync", method: nameof(SyncEndpointAsync), endpoint: sourceEndpoint);
//            }
//        }
//    }

//    private async Task<List<JsonElement>> GetEndpointItemsAsync(string endpoint, int pageSize, CancellationToken cancellationToken)
//    {
//        var allItems = new List<JsonElement>();
//        int page = 1;

//        while (true)
//        {
//            cancellationToken.ThrowIfCancellationRequested();

//            string url = $"{_storesace.BaseUrl}{endpoint}/?format=json&page_size={pageSize}&page={page}";
//            var response = await _storesace.HttpClient.GetAsync(url, cancellationToken);
//            response.EnsureSuccessStatusCode();

//            var json = await response.Content.ReadAsStringAsync(cancellationToken);
//            using var doc = JsonDocument.Parse(json);

//            var items = doc.RootElement.GetProperty("results").EnumerateArray().ToList();
//            if (!items.Any())
//                break;

//            // Clona cada item para evitar ObjectDisposedException
//            foreach (var item in items)
//                allItems.Add(JsonDocument.Parse(item.GetRawText()).RootElement);

//            if (items.Count < pageSize)
//                break;

//            page++;
//        }

//        return allItems;
//    }
//}
