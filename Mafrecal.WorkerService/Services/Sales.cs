using Mafrecal.WorkerService.Data;
using Mafrecal.WorkerService.Helpers;
using Mafrecal.WorkerService.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mafrecal.WorkerService.Services
{

    public class Sales
    {

        public static async Task ProcessSale(
        JsonElement tx,
        PrimaveraService primavera,
        SqlService sql,
        StoresaceService storesace,
        CancellationToken cancellationToken, bool reprocess = false)
        {


            PrimaveraResponse? result;
            bool? exists;
            long synccounter;

            var mainSourceId = tx.GetProperty("ContractReferenceNumber").GetString();
            string sourceEndpoint = "";
            dynamic sourceId = "";
 
            dynamic transDocument = tx.GetProperty("TransDocument").GetString();

            //if (mainSourceId!= "24.20251202.FT.133694798")
            //{
            //    return;
            //}
 
                //if (transDocument =="FT"  || transDocument == "NC")
                //{
                //AppLogger.Error($"Tipo documento ignorado {transDocument} {mainSourceId}",
                //    endpoint: sourceEndpoint,
                //    sourceId: mainSourceId,
                //    source: "Mafrecal.WorkerService",
                //    ex: "O mapeamento do documento de integração não foi encontrado");
                //return;
                //}
 
            #region CLIENTE

            if (!await PreProcessCustomer(tx, primavera, sql, storesace, cancellationToken))
                return;


            #endregion

            #region ARTIGOS

            sourceEndpoint = "items";

            foreach (var item in tx.GetProperty("SaleTransactionDetails").EnumerateArray())
            {
                sourceId = item.GetProperty("ItemId").GetString()!;
                synccounter = tx.GetProperty("synccounter").GetInt64();

                exists = await sql.ExistsStringAsync(sourceEndpoint, sourceId, synccounter);

                if (exists != null)
                {
                    if (exists == true)
                    {
                        continue;
                    }
                }
                else
                {
                    await sql.SaveTransactionAsync(
                    sourceEndpoint,
                    sourceId,
                    item.GetRawText(),
                    synccounter);
                }

                var artigoJson = MapperService.MapArtigoGroup(item);

                result = await primavera.PostAsync(
                    "Artigos/Actualiza",
                    artigoJson);

                if (result.Success)
                {
                    await sql.MarkAsProcessedAsync(sourceEndpoint, sourceId, cancellationToken);
                    AppLogger.Info($"{sourceEndpoint} {sourceId} criado/actualizado.",
                        endpoint: sourceEndpoint,
                        sourceId: sourceId,
                        source: "Mafrecal.WorkerService");
                }
                else
                {
                    string errorMessage = JsonHelper.BuildErrorMessage(result);
                    await sql.MarkAsErrorAsync(sourceEndpoint, sourceId, errorMessage, cancellationToken);
                    AppLogger.Error($"Erro ao sincronizar transação  MainSourceId {mainSourceId} {sourceEndpoint} {sourceId}",
                       endpoint: sourceEndpoint,
                       sourceId: sourceId,
                       source: "Mafrecal.WorkerService",
                       ex: errorMessage);
                    return;
                }
            }

            #endregion

            #region VENDAS

            sourceEndpoint = "sales";

            synccounter = tx.GetProperty("synccounter").GetInt64();

            exists = await sql.ExistsIntAsync(sourceEndpoint, Convert.ToString(mainSourceId), synccounter);

            if (exists != null)
            {
                if (exists == true)
                {
                    return;
                }
            }
            else
            {
                await sql.SaveTransactionAsync(
                        sourceEndpoint,
                        mainSourceId,
                        tx.GetRawText(),
                        synccounter);
            }

            var documentSale = await sql.DocumentConfig(
            "V",
            tx.GetProperty("TransDocument").GetString(),
            tx.GetProperty("StoreId").GetString(),
            1,
            cancellationToken);



            var caixa = await sql.DocumentConfig(
                "B",
                 "",
                tx.GetProperty("StoreId").GetString(),
                1,
                cancellationToken);

            var documentFecho = await sql.DocumentConfig(
                "T",
                "",
                tx.GetProperty("StoreId").GetString(),
                1,
                cancellationToken);

            if (string.IsNullOrEmpty(documentSale) || string.IsNullOrEmpty(caixa) || string.IsNullOrEmpty(documentFecho))
            {
                AppLogger.Error($"Erro ao sincronizar transação {mainSourceId}",
                    endpoint: sourceEndpoint,
                    sourceId: mainSourceId,
                    source: "Mafrecal.WorkerService",
                    ex: "O mapeamento do documento de integração não foi encontrado");

                await sql.MarkAsErrorAsync(sourceEndpoint, mainSourceId, "O mapeamento do documento de integração não foi encontrado", cancellationToken);
                return;
            }

            var vendaJson = MapperService.MapVendaGrouped(tx, documentSale, documentFecho, caixa);
      
  
                result = await primavera.PostAsync(
                "Vendas/Docs/CreateDocument",
                vendaJson
                );
          

            if (result.Success)
            {
                await sql.MarkAsProcessedAsync(sourceEndpoint, mainSourceId, cancellationToken);
                AppLogger.Info($"Transação {mainSourceId} sincronizada.",
                    endpoint: sourceEndpoint,
                    sourceId: mainSourceId,
                    source: "Mafrecal.WorkerService");

                if (reprocess)
                    await sql.MarkReprocessAsDoneAsync(Convert.ToInt32(mainSourceId), sourceEndpoint);
            }
            else
            {
                string errorMessage = JsonHelper.BuildErrorMessage(result);
                await sql.MarkAsErrorAsync(sourceEndpoint, mainSourceId, errorMessage, cancellationToken);
                AppLogger.Error($"Erro ao sincronizar transação {sourceEndpoint} {mainSourceId}",
                    endpoint: sourceEndpoint,
                   sourceId: mainSourceId,
                   source: "Mafrecal.WorkerService",
                   ex: errorMessage);

                if (reprocess)
                    await sql.MarkReprocessAsErrorAsync(Convert.ToInt32(mainSourceId), errorMessage, sourceEndpoint);



            }

            #endregion
        }

        private static async Task<bool> PreProcessCustomer(
       JsonElement tx,
       PrimaveraService primavera,
       SqlService sql,
       StoresaceService storesace,
       CancellationToken cancellationToken)
        {
            PrimaveraResponse? result;
            bool? exists;
            long synccounter;

            string sourceEndpoint = "customers";
            dynamic sourceId = tx.GetProperty("CustomerTaxId").GetString()!;
            var mainSourceId = tx.GetProperty("ContractReferenceNumber").GetString();

            if (string.IsNullOrEmpty(sourceId))
            {
                sourceId = "VD";
                //AppLogger.Error(
                //    $"{sourceEndpoint} {sourceId} Identificador do cliente vazio.",
                //    endpoint: sourceEndpoint,
                //    sourceId: mainSourceId,
                //    source: "Mafrecal.WorkerService");
                //return false;
            }

            var customerStoresace =
                await storesace.GetCustomerByIdAsync(sourceId, cancellationToken);

            if (customerStoresace is null)
            {
                string msg = $"{sourceEndpoint} {sourceId} não encontrado.";
                AppLogger.Error(msg, source: "Mafrecal.WorkerService");
                await sql.MarkAsErrorAsync(sourceEndpoint, sourceId, msg, cancellationToken);
                return false;
            }

            JsonElement root = (JsonElement)customerStoresace;

            if (!root.TryGetProperty("synccounter", out JsonElement syncProp))
            {
                AppLogger.Error(
                    $"{sourceEndpoint} {sourceId} Propriedade 'synccounter' não existe.",
                    endpoint: sourceEndpoint,
                    sourceId: sourceId,
                    source: "Mafrecal.WorkerService");

                return false;
            }

            synccounter = customerStoresace.GetProperty("synccounter").GetInt64();
            exists = await sql.ExistsStringAsync(sourceEndpoint, sourceId, synccounter);

            if (exists == true)
            {
                return true;
            }
            else if (exists == false)
            {
                if (await ProcessCustomer(tx, primavera, sql, customerStoresace, cancellationToken))
                    return true;
            }
            else
            {
                await sql.SaveTransactionAsync(
                   sourceEndpoint,
                   sourceId,
                   customerStoresace.GetRawText(),
                   synccounter);

                if (await ProcessCustomer(tx, primavera, sql, customerStoresace, cancellationToken))
                    return true;
            }
            return false;
        }

        private static async Task<bool> ProcessCustomer(
       JsonElement tx,
       PrimaveraService primavera,
       SqlService sql,
        JsonElement customerStoresace,
       CancellationToken cancellationToken)
        {
            PrimaveraResponse? result;
            bool? exists;
            long synccounter;

            string sourceEndpoint = "customers";
            dynamic sourceId = tx.GetProperty("CustomerTaxId").GetString()!;
            var mainSourceId = tx.GetProperty("ContractReferenceNumber").GetString();

            if (string.IsNullOrEmpty(sourceId))
            {
                sourceId = "VD";
                //AppLogger.Error(
                //    $"{sourceEndpoint} {sourceId} Identificador do cliente vazio.",
                //    endpoint: sourceEndpoint,
                //    sourceId: mainSourceId,
                //    source: "Mafrecal.WorkerService");
                //return false;
            }

            //var modoPagmentoJson =
            //        MapperService.CondPagamento(tx);

            //result = await primavera.PostAsync(
            //       "Base/CondPagamento",
            //       modoPagmentoJson);

            //if (!result.Success)
            //{
            //    string errorMessage = JsonHelper.BuildErrorMessage(result);

            //    await sql.MarkAsErrorAsync(sourceEndpoint, sourceId, errorMessage, cancellationToken);
            //    AppLogger.Error($"Erro ao sincronizar CondPagamento MainSourceId {mainSourceId} {sourceEndpoint} {sourceId}",
            //     endpoint: sourceEndpoint,
            //     sourceId: sourceId,
            //     source: "Mafrecal.WorkerService",
            //     ex: errorMessage);

            //    return false;
            //}

            var customerJson =
                        MapperService.MapCliente(customerStoresace);

            result = await primavera.PostAsync(
                "Clientes/Actualiza",
                customerJson);

            if (result.Success)
            {
                await sql.MarkAsProcessedAsync(sourceEndpoint, sourceId, cancellationToken);
                AppLogger.Info($"{sourceEndpoint} {sourceId} criado/atualizado.",
                    endpoint: sourceEndpoint,
                    sourceId: sourceId,
                    source: "Mafrecal.WorkerService");
                return true;
            }
            else
            {
                string errorMessage = JsonHelper.BuildErrorMessage(result);
                await sql.MarkAsErrorAsync(sourceEndpoint, sourceId, errorMessage, cancellationToken);
                AppLogger.Error($"Erro ao sincronizar transação MainSourceId {mainSourceId} {sourceEndpoint} {sourceId}",
                 endpoint: sourceEndpoint,
                 sourceId: sourceId,
                 source: "Mafrecal.WorkerService",
                 ex: errorMessage);
                return false;
            }

        }

    }


}
