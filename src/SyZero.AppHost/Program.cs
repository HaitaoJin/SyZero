using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
const int example5FrontendPort = 5175;

var example1 = builder.AddProject<Projects.SyZero_Example1_Web>("example1");
var example2 = builder.AddProject<Projects.SyZero_Example2_Web>("example2");
var example3 = builder.AddProject<Projects.SyZero_Example3_Web>("example3");

var gateway = builder.AddProject<Projects.SyZero_Gateway>("gateway")
    .WithReference(example1)
    .WithReference(example2);

builder.AddViteApp("example5-frontend", "../SyZero.Service/SyZero.Example5.Frontend")
    .WithHttpEndpoint(
        targetPort: example5FrontendPort,
        port: example5FrontendPort,
        isProxied: false)
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
