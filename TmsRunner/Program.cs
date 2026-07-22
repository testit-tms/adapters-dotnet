using System.Globalization;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Settings.Configuration;
using System.Net;
using TestIT.AdaptersApi.Api;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Utils;
using TmsRunner.Entities;
using TmsRunner.Entities.Configuration;
using TmsRunner.Enums;
using TmsRunner.Extensions;
using TmsRunner.Handlers;
using TmsRunner.Managers;
using TmsRunner.Services;
using TmsRunner.Services.Implementations;
using TmsRunner.Utils;
using ConfigurationManager = TmsRunner.Managers.ConfigurationManager;

namespace TmsRunner;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var host = CreateHostBuilder(args).Build();
        return await host.Services.GetRequiredService<App>().RunAsync().ConfigureAwait(false);
    }

    private static AdapterConfig GetAdapterConfiguration(IEnumerable<string> args)
    {
        AdapterConfig config = null!;

        _ = Parser.Default.ParseArguments<AdapterConfig>(args)
            .WithParsed(ac =>
            {
                config = new AdapterConfig
                {
                    RunnerPath = ac.RunnerPath?.RemoveQuotes(),
                    TestAssemblyPath = ac.TestAssemblyPath?.RemoveQuotes(),
                    TestAdapterPath = ac.TestAdapterPath?.RemoveQuotes() ?? string.Empty,
                    LoggerPath = ac.LoggerPath?.RemoveQuotes() ?? string.Empty,
                    IsDebug = ac.IsDebug,
                    TmsUrl = ac.TmsUrl,
                    TmsPrivateToken = ac.TmsPrivateToken,
                    TmsProjectId = ac.TmsProjectId,
                    TmsConfigurationId = ac.TmsConfigurationId,
                    TmsTestRunId = ac.TmsTestRunId,
                    TmsTestRunName = ac.TmsTestRunName,
                    TmsAdapterMode = ac.TmsAdapterMode,
                    TmsConfigFile = ac.TmsConfigFile,
                    TmsLabelsOfTestsToRun = ac.TmsLabelsOfTestsToRun,
                    TmsAutomaticCreationTestCases = ac.TmsAutomaticCreationTestCases,
                    TmsRunSettings = ac.TmsRunSettings,
                    TmsAutomaticUpdationLinksToTestCases = ac.TmsAutomaticUpdationLinksToTestCases,
                    TmsCertValidation = ac.TmsCertValidation,
                    TmsRerunTestsCount = ac.TmsRerunTestsCount,
                    TmsIgnoreParameters = ac.TmsIgnoreParameters,
                    TmsSyncStoragePort = ac.TmsSyncStoragePort
                };
            });

        if (string.IsNullOrWhiteSpace(config.TmsRunSettings))
        {
            config.TmsRunSettings =
        @"
<RunSettings>
  <RunConfiguration>
  </RunConfiguration> 
</RunSettings>
";
        }

        return config;
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        var adapterConfig = GetAdapterConfiguration(args);
        var options = new ConfigurationReaderOptions(
            typeof(ConsoleLoggerConfigurationExtensions).Assembly,
            typeof(SerilogExpression).Assembly
        );

        return Host.CreateDefaultBuilder()
            .UseSerilog((context, services, configuration) =>
            {
                var consoleLogLevel = adapterConfig.IsDebug ? LogEventLevel.Debug : LogEventLevel.Information;
                
                configuration
                    .ReadFrom.Configuration(context.Configuration, options)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                    .WriteTo.Console(consoleLogLevel, formatProvider: CultureInfo.InvariantCulture);
            })
            .ConfigureServices((hostContext, services) =>
            {
                _ = services.AddHttpClient(nameof(HttpClientNames.Default), client =>
                {
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                    client.DefaultRequestVersion = HttpVersion.Version11;
                })
                .SetHandlerLifetime(TimeSpan.FromDays(1))
                .ConfigurePrimaryHttpMessageHandler(provider =>
                    AdaptersApiConfiguration.CreateHandler(new Tms.Adapter.Core.Configurator.TmsSettings
                    {
                        CertValidation = provider.GetRequiredService<TmsSettings>().CertValidation
                    }))
                .AddPolicyHandler(GetRetryPolicy());

                _ = services
                    .AddSingleton(adapterConfig)
                    .AddSingleton(provider =>
                    {
                        var adpConfig = provider.GetRequiredService<AdapterConfig>();

                        return ConfigurationManager.Configure(
                            adpConfig.ToInternalConfig(),
                            Path.GetDirectoryName(adpConfig.TestAssemblyPath) ?? string.Empty
                        );
                    })
                    .AddSingleton(provider =>
                    {
                        var tmsSettings = provider.GetRequiredService<TmsSettings>();
                        var basePath = AdaptersApiConfiguration.NormalizeBaseUrl(tmsSettings.Url);

                        if (AdaptersApiConfiguration.IsTraceEnabled())
                        {
                            Console.WriteLine($"[TMS] Adapters API BasePath={basePath}");
                        }

                        return new TestIT.AdaptersApi.Client.Configuration
                        {
                            BasePath = basePath,
                            ApiKeyPrefix = new Dictionary<string, string> { { "Authorization", "PrivateToken" } },
                            ApiKey = new Dictionary<string, string> { { "Authorization", tmsSettings.PrivateToken ?? string.Empty } }
                        };
                    })
                    .AddTransient(provider => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) =>
                            provider.GetRequiredService<TmsSettings>().CertValidation
                    })
                    .AddTransient<IAttachmentsApiAsync, AttachmentsApi>(provider =>
                    {
                        var api = new AttachmentsApi(
                            provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(HttpClientNames.Default)),
                            provider.GetRequiredService<TestIT.AdaptersApi.Client.Configuration>(),
                            provider.GetRequiredService<HttpClientHandler>());
                        AdaptersApiConfiguration.ApplyExceptionFactory(api);
                        return api;
                    })
                    .AddTransient<ITestRunsApiAsync, TestRunsApi>(provider =>
                    {
                        var api = new TestRunsApi(
                            provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(HttpClientNames.Default)),
                            provider.GetRequiredService<TestIT.AdaptersApi.Client.Configuration>(),
                            provider.GetRequiredService<HttpClientHandler>());
                        AdaptersApiConfiguration.ApplyExceptionFactory(api);
                        return api;
                    })
                    .AddTransient<ITestResultsApiAsync, TestResultsApi>(provider =>
                    {
                        var api = new TestResultsApi(
                            provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(HttpClientNames.Default)),
                            provider.GetRequiredService<TestIT.AdaptersApi.Client.Configuration>(),
                            provider.GetRequiredService<HttpClientHandler>());
                        AdaptersApiConfiguration.ApplyExceptionFactory(api);
                        return api;
                    })
                    .AddTransient<IAutoTestsApiAsync, AutoTestsApi>(provider =>
                    {
                        var api = new AutoTestsApi(
                            provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(HttpClientNames.Default)),
                            provider.GetRequiredService<TestIT.AdaptersApi.Client.Configuration>(),
                            provider.GetRequiredService<HttpClientHandler>());
                        AdaptersApiConfiguration.ApplyExceptionFactory(api);
                        return api;
                    })
                    .AddTransient<IProjectsApiAsync, ProjectsApi>(provider =>
                    {
                        var api = new ProjectsApi(
                            provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(HttpClientNames.Default)),
                            provider.GetRequiredService<TestIT.AdaptersApi.Client.Configuration>(),
                            provider.GetRequiredService<HttpClientHandler>());
                        AdaptersApiConfiguration.ApplyExceptionFactory(api);
                        return api;
                    })
                    .AddTransient<App>()
                    .AddTransient<TmsManager>()
                    .AddTransient<FilterService>()
                    .AddSingleton<ProcessorService>()
                    .AddTransient<EventWaitHandle>(_ => new AutoResetEvent(false))
                    .AddTransient<DiscoveryEventHandler>()
                    .AddTransient<RunEventHandler>()
                    .AddTransient<IVsTestConsoleWrapper, VsTestConsoleWrapper>(provider => new VsTestConsoleWrapper(
                        provider.GetRequiredService<AdapterConfig>().RunnerPath ?? string.Empty,
                        new ConsoleParameters { LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "log.txt") }
                    ))
                    .AddTransient<RunService>()
                    .AddSingleton<ITestRunContextService, TestRunContextService>() // we need to keep this singleton ofr entire app context 
                    .AddSingleton<SyncStorageSession>();
            });
    }

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}