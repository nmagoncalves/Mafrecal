using Mafrecal.WorkerService.Data;
using Mafrecal.WorkerService.Helpers;
using Mafrecal.WorkerService.Logging;
using Mafrecal.WorkerService.Services;
 
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;



namespace Mafrecal.WorkerService
{


    public class Worker : BackgroundService
    {
        private readonly IConfiguration _config;

        public Worker(IConfiguration config) => _config = config;

        public record EndpointConfig(int Interval, string Type);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            AppLogger.Initialize(_config["SQL:ConnectionString"]);

            var primavera = new PrimaveraService(_config["Primavera:BaseUrl"]);
            var sql = new SqlService(_config["SQL:ConnectionString"]);



            var storesace = new StoresaceService(
                _config["Storesace:BaseUrl"],
                _config["Storesace:User"],
                _config["Storesace:Password"],
                _config["Storesace:LookbackDays"],
                sql
            );

            var auth = new PrimaveraAuthService(
                _config["Primavera:Url"],
                _config["Primavera:AuthUrl"],
                _config["Primavera:Username"],
                _config["Primavera:Password"],
                _config["Primavera:Company"],
                _config["Primavera:Instance"],
                 _config["Primavera:Line"]
            );

 

            var endpointsConfig = _config
                .GetSection("Storesace:Endpoints")
                .Get<Dictionary<string, EndpointConfig>>();

            var schedules = new List<EndpointSchedule>();

            //var endpointsConfig = _config
            //    .GetSection("Storesace:Endpoints")
            //    .Get<Dictionary<string, int>>();


            foreach (var ep in endpointsConfig)
            {
                 var name = ep.Key;
                //var interval = ep.Value;

                var endpoint = endpointsConfig[name];

                Func<JsonElement, PrimaveraService, SqlService, Task> processor = name switch
                {
                    "purchases" => (tx, p, s) => Purchases.ProcessPurchaseGroup(tx, p, s, storesace, stoppingToken),
                    "purchases/full" => (tx, p, s) => Purchases.ProcessPurchaseFull(tx, p, s, storesace, stoppingToken),
                    "sales/resume/dttn" => (tx, p, s) => Sales.ProcessSale(tx, p, s, storesace, stoppingToken),
                    "wastemovements" => (tx, p, s) => Interns.ProcessIntern(tx, p, s, storesace, stoppingToken),
                    //"interns" => (tx, p, s) => ProcessIntern(tx, p, s, stoppingToken),
                    _ => throw new NotSupportedException($"Endpoint {name} não suportado")
                };

                schedules.Add(new EndpointSchedule
                {
                    Name = name,
                    IntervalMinutes =  endpoint.Interval,
                    NextRun = DateTime.UtcNow,
                    Processor = processor,
                    Type = endpoint.Type

                });
            }

            // LOOP ÚNICO
            while (!stoppingToken.IsCancellationRequested)
            {

                // Token pedido UMA VEZ
                var token = await auth.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    AppLogger.Error("Token inválido – ciclo abortado", source: "Worker");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                primavera.SetToken(token);


                // 1 REPROCESSAMENTOS (prioridade máxima)
                var requests = await sql.GetPendingReprocessRequestsAsync(stoppingToken);

                foreach (var req in requests)
                {
                    try
                    {
                        await sql.MarkReprocessAsRunningAsync(req.Id);

                        using var doc = JsonDocument.Parse(req.JsonData);
                        var tx = doc.RootElement;

                        Func<JsonElement, PrimaveraService, SqlService, Task> processor = req.SourceEndpoint switch
                        {
                            "purchases" => (tx, p, s) => Purchases.ProcessPurchaseGroup(tx, p, s, storesace, stoppingToken, true),
                            "purchasesfull" => (tx, p, s) => Purchases.ProcessPurchaseFull(tx, p, s, storesace, stoppingToken, true),
                            "sales/resume/dttn" => (tx, p, s) => Sales.ProcessSale(tx, p, s, storesace, stoppingToken, true),
                            "wastemovements" => (tx, p, s) => Interns.ProcessIntern(tx, p, s, storesace, stoppingToken, true),
                            _ => throw new NotSupportedException($"Modo {req.SourceEndpoint} não suportado")
                        };

                        await processor(tx, primavera, sql);

                   //     await sql.MarkReprocessAsDoneAsync(req.Id);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Execução Reprocessamento", endpoint: "Reprocessamento",ex: ex.ToString());
                        await sql.MarkReprocessAsErrorAsync(req.Id, ex.ToString(), req.SourceEndpoint);
                    }
                }

                foreach (var schedule in schedules)
                {
                    if (DateTime.UtcNow < schedule.NextRun)
                        continue;

                    try
                    {
                        AppLogger.Info($"Execução endpoint {schedule.Name}", endpoint: schedule.Name);

                        var items = schedule.Type switch
                        {
                            "C" => await storesace.GetEndpointPurchasesAsync(
                                schedule.Name,
                                5000,
                                stoppingToken
                            ),
                            "I" => await storesace.GetEndpointPurchasesAsync(
                                schedule.Name,
                                5000,
                                stoppingToken
),

                            "V" => await storesace.GetEndpointSalesAsync(
                                schedule.Name,
                                5000,
                                stoppingToken
                            ),

                            _ => throw new NotSupportedException($"Tipo {schedule.Type} não suportado")
                        };


                        foreach (var json in items)
                        {
                            using var doc = JsonDocument.Parse(json);
                            await schedule.Processor(doc.RootElement, primavera, sql);
                        }

                        AppLogger.Info($"Endpoint {schedule.Name} finalizado", endpoint: schedule.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"{ex}");
                        AppLogger.Error(
                            "Erro no endpoint",
                            ex: ex.ToString(),
                            endpoint: schedule.Name
                        );
                    }

                    schedule.NextRun = DateTime.UtcNow.AddMinutes(schedule.IntervalMinutes);
                }

                // pequeno sleep para não ocupar CPU
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        class EndpointSchedule
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public int IntervalMinutes { get; set; }
            public DateTime NextRun { get; set; }
            public Func<JsonElement, PrimaveraService, SqlService, Task> Processor { get; set; }
        }
 

        // ############# para endpoints paralelos #############


        //        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //        {



        //            AppLogger.Initialize(_config["SQL:ConnectionString"]);

        //            var storesace = new StoresaceService(
        //                _config["Storesace:BaseUrl"],
        //                _config["Storesace:User"],
        //                _config["Storesace:Password"]
        //            );

        //            var auth = new PrimaveraAuthService(
        //                _config["Primavera:AuthUrl"],
        //                _config["Primavera:Username"],
        //                _config["Primavera:Password"],
        //                _config["Primavera:Company"],
        //                _config["Primavera:Instance"]
        //            );

        //            var primavera = new PrimaveraService(_config["Primavera:BaseUrl"]);
        //            var sql = new SqlService(_config["SQL:ConnectionString"]);

        //            Logger.Info(
        //                "Ligação ao SQL estabelecida com sucesso."
        //            );

        //            var endpoints = _config.GetSection("Storesace:Endpoints").Get<Dictionary<string, int>>();

        //            var tasks = new List<Task>();

        //          //  Obter token uma vez no início
        //            string token = await auth.GetTokenAsync();
        //            if (string.IsNullOrEmpty(token))
        //            {
        //                AppLogger.Error(
        //                    "Não foi possível obter o token de autenticação.",
        //                    ex: "",
        //                    source: "Mafrecal.WorkerService",
        //                    endpoint: "Inicialização"
        //                );

        //             //   Console.WriteLine("Não foi possível obter o token de autenticação. Verifique os logs.");
        //                 return; // termina o worker
        //            }
        //            primavera.SetToken(token); // define token no PrimaveraService

        //            // Daily sync
        //            //var dailySync = new DailySyncProcessor(storesace, primavera, sql);
        //            //tasks.Add(Task.Run(() => dailySync.RunDailySyncAsync(stoppingToken), stoppingToken));

        //            foreach (var ep in endpoints)
        //            {
        //                string endpointName = ep.Key;
        //                int intervalMinutes = ep.Value;

        //                //Func<JsonElement, PrimaveraService, SqlService, Task> processor = endpointName == "purchases"
        //                //    ? async (tx, p, s) => await ProcessPurchase(tx, p, s)
        //                //    : async (tx, p, s) => await ProcessSale(tx, p, s);

        //                var processors = new Dictionary<string, Func<JsonElement, PrimaveraService, SqlService, Task>>
        //{
        //                    { "purchases", async (tx, p, s) => await ProcessPurchaseGroup(tx, p, s, storesace, stoppingToken) },
        //                    { "purchases/full", async (tx, p, s) => await ProcessPurchaseFull(tx, p, s, storesace, stoppingToken) },
        //                    { "sales", async (tx, p, s) => await ProcessSale(tx, p, s, stoppingToken) },
        //                    { "interns", async (tx, p, s) => await ProcessIntern(tx, p, s, stoppingToken) }
        //                };

        //                if (!processors.TryGetValue(endpointName, out var processor))
        //                    throw new NotSupportedException($"Endpoint '{endpointName}' não suportado");

        //                tasks.Add(Task.Run(() =>
        //                    new EndpointProcessor(
        //                        endpointName,
        //                        intervalMinutes,
        //                        storesace,
        //                        auth,
        //                        primavera,
        //                        sql,
        //                        processor
        //                    ).RunAsync(stoppingToken), stoppingToken));
        //            }

        //            await Task.Delay(Timeout.Infinite, stoppingToken);
        //           // await Task.WhenAll(tasks);

        //            //  Console.WriteLine("Finalizado");
        //        }
 
    }


}

