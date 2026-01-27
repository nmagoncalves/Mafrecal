using Mafrecal.WorkerService.Data;
using Mafrecal.WorkerService.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mafrecal.WorkerService.Services
{

    public class EndpointProcessor
    {
        private readonly string _endpoint;
        private readonly int _intervalMinutes;
        private readonly StoresaceService _storesace;
        private readonly PrimaveraAuthService _auth;
        private readonly PrimaveraService _primavera;
        private readonly SqlService _sql;
        private readonly Func<JsonElement, PrimaveraService, SqlService, Task> _processTransaction;

        public EndpointProcessor(
            string endpoint,
            int intervalMinutes,
            StoresaceService storesace,
            PrimaveraAuthService auth,
            PrimaveraService primavera,
            SqlService sql,
            Func<JsonElement, PrimaveraService, SqlService, Task> processTransaction
        )
        {
            _endpoint = endpoint;
            _intervalMinutes = intervalMinutes;
            _storesace = storesace;
            _auth = auth;
            _primavera = primavera;
            _sql = sql;
            _processTransaction = processTransaction;
        }



        //public async Task RunAsync(CancellationToken stoppingToken)
        //{
        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        try
        //        {

        //            AppLogger.Info(
        //                $"Execução endpoint {_endpoint}",
        //                source: "Mafrecal.WorkerService",
        //                endpoint: _endpoint);

        //            // Obtém todos os itens do endpoint via StoresaceService
        //            var transactionsJson = await _storesace.GetEndpointItemsAsync(_endpoint, 5000, stoppingToken);

        //            AppLogger.Info(
        //                    $"Endpoint {_endpoint}: {transactionsJson.Count} registos recebidos",
        //                    source: "Mafrecal.WorkerService",
        //                    endpoint: _endpoint
        //                );

        //            foreach (var txJson in transactionsJson)
        //            {
        //                using var doc = JsonDocument.Parse(txJson);
        //                var tx = doc.RootElement;
        //              //  Console.WriteLine("A processar!");
        //                // Processa cada transação (método existente)
        //                await _processTransaction(tx, _primavera, _sql);
        //            }

        //            AppLogger.Info(
        //             $"Endpoint {_endpoint} finalizado",
        //             source: "Mafrecal.WorkerService",
        //             endpoint: _endpoint);
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            // Cancelamento solicitado, sai do loop sem erro
        //            break;
        //        }
        //        catch (Exception ex)
        //        {
        //            AppLogger.Error(
        //                "Erro ao processar endpoint",
        //                ex: ex.ToString(),
        //                source: "Mafrecal.WorkerService",
        //                endpoint: _endpoint
        //            );
        //          //  Console.WriteLine($"Erro ao processar endpoint {_endpoint}: {ex.Message}");
        //        }

        //        // Aguarda o intervalo definido entre execuções
        //        try
        //        {
        //            await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            break; // cancelamento solicitado
        //        }
        //    }
        //}

    }

}
