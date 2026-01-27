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

    public class Purchases
    {

        public static async Task ProcessPurchaseGroup(
            JsonElement tx,
            PrimaveraService primavera,
            SqlService sql,
            StoresaceService storesace,
            CancellationToken cancellationToken,
            bool reprocess = false)
        {

            if (tx.TryGetProperty("TransMovement", out var tm) && tm.GetInt32() == 12)
            {
                return;
            }

            PrimaveraResponse? result;
            bool? exists;
            long synccounter;

            var mainSourceId = tx.GetProperty("Id").GetInt32();
            string sourceEndpoint = "";
            dynamic sourceId = "";

            #region FORNECEDOR

            if (!await PreProcessBuyer(tx, primavera, sql, storesace, cancellationToken))
                return;

            #endregion

            #region ARTIGOS

            sourceEndpoint = "items";

            foreach (var item in tx.GetProperty("BuyTransactionDetails").EnumerateArray())
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

            #region COMPRAS

            sourceEndpoint = "purchases";

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

            var document = await sql.DocumentConfig(
            "C",
            tx.GetProperty("TransDocument").GetString(),
            tx.GetProperty("StoreId").GetString(),
            1,
            cancellationToken);

            if (string.IsNullOrEmpty(document))
            {
                AppLogger.Error($"Erro ao sincronizar transação {mainSourceId}",
                    endpoint: sourceEndpoint,
                    sourceId: mainSourceId,
                    source: "Mafrecal.WorkerService",
                    ex: "O mapeamento do documento de integração não foi encontrado");

                await sql.MarkAsErrorAsync(sourceEndpoint, mainSourceId, "O mapeamento do documento de integração não foi encontrado", cancellationToken);
                return;
            }

            var compraJson = MapperService.MapCompraGrouped(tx, document);

            result = await primavera.PostAsync(
                "Compras/Docs/CreateDocument",
                compraJson,
                mainSourceId);

            if (result.Success)
            {
                await sql.MarkAsProcessedAsync(sourceEndpoint, mainSourceId, cancellationToken);
                AppLogger.Info($"Transação {mainSourceId} sincronizada.",
                    endpoint: sourceEndpoint,
                    sourceId: mainSourceId,
                    source: "Mafrecal.WorkerService");
                if (reprocess)
                    await sql.MarkReprocessAsDoneAsync(mainSourceId, sourceEndpoint);
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
                    await sql.MarkReprocessAsErrorAsync(mainSourceId, errorMessage, sourceEndpoint);

            }

            #endregion
        }

        public static async Task ProcessPurchaseFull(
            JsonElement tx,
            PrimaveraService primavera,
            SqlService sql,
            StoresaceService storesace,
            CancellationToken cancellationToken,
             bool reprocess = false)
        {


            if (tx.TryGetProperty("TransMovement", out var tm) && tm.GetInt32() != 12)
            {
                //AppLogger.Info($"{tx.GetProperty("Id").GetInt32()} não é do tipo 12",
                //endpoint: "",
                //sourceId: "",
                //source: "Mafrecal.WorkerService");
                return;
            }

            PrimaveraResponse? result;
            string sourceEndpoint = "";
            dynamic sourceId = "";

            bool? exists;
            long synccounter;

            var mainSourceId = tx.GetProperty("Id").GetInt32();


            #region FORNECEDORES

            if (!await PreProcessBuyer(tx, primavera, sql, storesace, cancellationToken))
                return;

            #endregion

            #region ARTIGOS

            sourceEndpoint = "items";

            foreach (var item in tx.GetProperty("BuyTransactionDetails").EnumerateArray())
            {


                sourceId = item.GetProperty("Item").ValueKind switch
                {
                    JsonValueKind.String => item.GetProperty("Item").GetString(),
                    JsonValueKind.Number => item.GetProperty("Item").GetInt64().ToString(),
                };

                var artigoStoresace =
                    await storesace.GetItemFullByIdAsync(sourceId, cancellationToken);

                if (artigoStoresace is null)
                {
                    string msg = $"{sourceEndpoint} {sourceId} não encontrado.";
                    AppLogger.Error(msg, source: "Mafrecal.WorkerService");
                    await sql.MarkAsErrorAsync(sourceEndpoint, sourceId, msg, cancellationToken);
                    return;
                }

                synccounter = artigoStoresace.GetProperty("synccounter").GetInt64();
                exists = await sql.ExistsIntAsync(sourceEndpoint, Convert.ToString(sourceId), synccounter);

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

                var artigoJson =
                    MapperService.MapArtigoFull(artigoStoresace, tx);

                result = await primavera.PostAsync(
                    "Artigos/Actualiza",
                    artigoJson);

                if (result.Success)
                {
                    await sql.MarkAsProcessedAsync(sourceEndpoint, sourceId, cancellationToken);
                    AppLogger.Info($"{sourceEndpoint} {sourceId} criado/atualizado.",
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

            #region COMPRAS

            sourceEndpoint = "purchasesfull";

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


            var document = await sql.DocumentConfig(
                "C",
                tx.GetProperty("TransDocument").GetString(),
                tx.GetProperty("StoreId").GetString(),
                0,
                cancellationToken);

            if (string.IsNullOrEmpty(document))
            {
                AppLogger.Error($"Erro ao sincronizar transação {mainSourceId}",
                    endpoint: sourceEndpoint,
                    sourceId: mainSourceId,
                    source: "Mafrecal.WorkerService",
                    ex: "O mapeamento do documento de integração não foi encontrado");
                await sql.MarkAsErrorAsync(sourceEndpoint, mainSourceId, "O mapeamento do documento de integração não foi encontrado", cancellationToken);

                return;
            }


            var compraJson = MapperService.MapCompraFull(tx, document);

            result = await primavera.PostAsync(
                "Compras/Docs/CreateDocument",
                compraJson,
                mainSourceId);

            if (result.Success)
            {
                await sql.MarkAsProcessedAsync(sourceEndpoint, mainSourceId, cancellationToken);
                AppLogger.Info($"Transação {mainSourceId} sincronizada.",
                    sourceId: mainSourceId,
                    source: "Mafrecal.WorkerService",
                    endpoint: sourceEndpoint);

                if (reprocess)
                    await sql.MarkReprocessAsDoneAsync(mainSourceId, sourceEndpoint);
            }
            else
            {
                string errorMessage = JsonHelper.BuildErrorMessage(result);
                await sql.MarkAsErrorAsync(sourceEndpoint, mainSourceId, errorMessage, cancellationToken);
                AppLogger.Error($"Erro ao sincronizar transação {mainSourceId}",
                    sourceId: mainSourceId,
                    source: "Mafrecal.WorkerService",
                    endpoint: sourceEndpoint,
                    ex: errorMessage);

                if (reprocess)
                    await sql.MarkReprocessAsErrorAsync(mainSourceId, errorMessage, sourceEndpoint);

            }

            #endregion
        }

        private static async Task<bool> PreProcessBuyer(
               JsonElement tx,
               PrimaveraService primavera,
               SqlService sql,
               StoresaceService storesace,
               CancellationToken cancellationToken)
        {
            PrimaveraResponse? result;
            bool? exists;
            long synccounter;

            string sourceEndpoint = "suppliers";
            dynamic sourceId = tx.GetProperty("SupplierId").GetString()!;
            var mainSourceId = tx.GetProperty("Id").GetInt32();

            var fornecedorStoresace =
                await storesace.GetSupplierByIdAsync(sourceId, cancellationToken);

            if (fornecedorStoresace is null)
            {
                string msg = $"{sourceEndpoint} {sourceId} não encontrado.";
                AppLogger.Error(msg, source: "Mafrecal.WorkerService");
                await sql.MarkAsErrorAsync(sourceEndpoint, sourceId, msg, cancellationToken);
                return false;
            }

            JsonElement root = (JsonElement)fornecedorStoresace;

            if (!root.TryGetProperty("synccounter", out JsonElement syncProp))
            {
                AppLogger.Error(
                    $"{sourceEndpoint} {sourceId} Propriedade 'synccounter' não existe.",
                    endpoint: sourceEndpoint,
                    sourceId: sourceId,
                    source: "Mafrecal.WorkerService");

                return false;
            }

            synccounter = fornecedorStoresace.GetProperty("synccounter").GetInt64();
            exists = await sql.ExistsStringAsync(sourceEndpoint, sourceId, synccounter);

            if (exists == true)
            {
                return true;
            }
            else if (exists == false)
            {
                if (await ProcessBuyer(tx, primavera, sql, fornecedorStoresace, cancellationToken))
                    return true;
            }
            else
            {
                await sql.SaveTransactionAsync(
                   sourceEndpoint,
                   sourceId,
                   fornecedorStoresace.GetRawText(),
                   synccounter);

                if (await ProcessBuyer(tx, primavera, sql, fornecedorStoresace, cancellationToken))
                    return true;
            }
            return false;
        }

        private static async Task<bool> ProcessBuyer(
       JsonElement tx,
       PrimaveraService primavera,
       SqlService sql,
        JsonElement fornecedorStoresace,
       CancellationToken cancellationToken)
        {
            PrimaveraResponse? result;
            bool? exists;
            long synccounter;

            string sourceEndpoint = "suppliers";
            dynamic sourceId = tx.GetProperty("SupplierId").GetString()!;
            var mainSourceId = tx.GetProperty("Id").GetInt32();

            var modoPagmentoJson =
                    MapperService.CondPagamento(tx);

            result = await primavera.PostAsync(
                   "Base/CondPagamento",
                   modoPagmentoJson);

            if (!result.Success)
            {
                string errorMessage = JsonHelper.BuildErrorMessage(result);

                await sql.MarkAsErrorAsync(sourceEndpoint, sourceId, errorMessage, cancellationToken);
                AppLogger.Error($"Erro ao sincronizar CondPagamento MainSourceId {mainSourceId} {sourceEndpoint} {sourceId}",
                 endpoint: sourceEndpoint,
                 sourceId: sourceId,
                 source: "Mafrecal.WorkerService",
                 ex: errorMessage);

                return false;
            }

            var fornecedorJson =
                        MapperService.MapFornecedor(fornecedorStoresace, tx);

            result = await primavera.PostAsync(
                "Fornecedores/Actualiza",
                fornecedorJson);

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
