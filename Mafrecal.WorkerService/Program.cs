using Mafrecal.WorkerService;
using Mafrecal.WorkerService.Logging;


//IHost host = Host.CreateDefaultBuilder(args)
//    .ConfigureServices((context, services) =>
//    {
//        services.AddHostedService<Worker>();
//    })
//    .Build();

//await host.RunAsync();



IHost host = Host.CreateDefaultBuilder(args)
      .UseWindowsService(options =>
      {
          options.ServiceName = "Mafrecal Integration Service";
      })
    .ConfigureServices((context, services) =>
    {

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();