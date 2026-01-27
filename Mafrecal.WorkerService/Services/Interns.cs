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

    public class Interns    
    {


        public static async Task ProcessIntern(
            JsonElement tx,
            PrimaveraService primavera,
            SqlService sql,
            StoresaceService storesace,
            CancellationToken cancellationToken, bool reprocess = false)
        {


            PrimaveraResponse? result;
            bool? exists;
            long synccounter;

            var mainSourceId = tx.GetProperty("Id").GetInt32();
            string sourceEndpoint = "";
            dynamic sourceId = "";


            //if (!await PreProcessCustomer(tx, primavera, sql, storesace, cancellationToken))
            //    return;


            sourceEndpoint = "items";

            foreach (var item in tx.GetProperty("TransactionDetails").EnumerateArray())
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

 

            sourceEndpoint = "wastemovements";

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

            var documentIntern = await sql.DocumentConfig(
            "I",
            "",
            tx.GetProperty("StoreId").GetString(),
            1,
            cancellationToken);

            // tx.GetProperty("TransDocument").GetString()

            if (string.IsNullOrEmpty(documentIntern))
            {
                AppLogger.Error($"Erro ao sincronizar transação {mainSourceId}",
                    endpoint: sourceEndpoint,
                    sourceId: mainSourceId,
                    source: "Mafrecal.WorkerService",
                    ex: "O mapeamento do documento de integração não foi encontrado");

                await sql.MarkAsErrorAsync(sourceEndpoint, mainSourceId, "O mapeamento do documento de integração não foi encontrado", cancellationToken);
                return;
            }

            var vendaJson = MapperService.MapInterno(tx, documentIntern);

            result = await primavera.PostAsync(
                "Internos/Docs/CreateDocument",
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
 
        }
 
    }
 
}
