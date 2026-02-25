using AgroSolution.Worker.Config;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkerServices(builder.Configuration);

var host = builder.Build();
host.Run();
