using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Description;
using WebAppCoreWCF.Soap;

namespace WebAppCoreWCF;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // CoreWCF uses some legacy APIs which require synchronous IO.
        // Without this you will typically get runtime errors when calling the SOAP endpoint.
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AllowSynchronousIO = true;
        });

        // Add services to the container.
        builder.Services.AddControllers();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        // CoreWCF: registers the service model runtime + metadata (WSDL) support.
        builder.Services.AddServiceModelServices()
            .AddServiceModelMetadata();

        // Makes generated WSDL/help-page use host/port from incoming request headers.
        // Important when behind reverse proxies / containers.
        builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // For simplest local testing, keep HTTP enabled (no forced redirect to HTTPS).
        // If you want HTTPS-only SOAP, configure a Transport-secured binding and enable redirection.
        app.UseAuthorization();

        // Enable WSDL publishing.
        var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
        serviceMetadataBehavior.HttpGetEnabled = true;
        serviceMetadataBehavior.HttpsGetEnabled = true;

        // Host SOAP endpoints in ASP.NET Core pipeline.
        app.UseServiceModel(serviceBuilder =>
        {
            var binding = new BasicHttpBinding();

            serviceBuilder
                .AddService<GreeterService>(serviceOptions =>
                {
                    serviceOptions.DebugBehavior.IncludeExceptionDetailInFaults = app.Environment.IsDevelopment();
                })
                .AddServiceEndpoint<GreeterService, IGreeterService>(
                    binding,
                    "/soap/GreeterService.svc");
        });

        app.MapControllers();
        app.Run();
    }
}
