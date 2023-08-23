using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Play.Common.Settings;
using Polly;
using Polly.Timeout;

namespace Play.Common.Clients
{
    public static class Extensions
    {
        public static IServiceCollection AddServiceCommunicationClient<T>(this IServiceCollection service)
        {
            /**service.AddSingleton<IClient<T>>(provider => // we have to add the correct httpClient to the service with the correct BaseAddress.
            {
                var configuration = provider.GetService<IConfiguration>();
                var serviceCommunicationSettings = configuration.GetSection(nameof(ServiceCommunicationSettings)).Get<ServiceCommunicationSettings>(); // the address will be retrieved from the appsetting.json file.
                HttpClient httpClient = new HttpClient
                {
                    BaseAddress = serviceCommunicationSettings.CommunicationUri,
                    Timeout = TimeSpan.FromSeconds(serviceCommunicationSettings.TimeoutSecs)
                };
                return new Client<T>(httpClient);
            });**/
            var provider = service.BuildServiceProvider();
            var configuration = provider.GetService<IConfiguration>();
            var serviceCommunicationSettings = configuration.GetSection(nameof(ServiceCommunicationSettings)).Get<ServiceCommunicationSettings>(); // the address will be retrieved from the appsetting.json file.
            service.AddHttpClient<IClient<T>, Client<T>>(client => client.BaseAddress = serviceCommunicationSettings.CommunicationUri)
                .AddTransientHttpErrorPolicy(build => build.Or<TimeoutRejectedException>().WaitAndRetryAsync(5, attemp => TimeSpan.FromSeconds(Math.Pow(2, attemp)),
                    onRetry: (outCome, timespan, retry) =>
                    {
                        var provider = service.BuildServiceProvider();
                        provider.GetService<ILogger<Client<T>>>()?.LogWarning($"Delay {timespan.TotalSeconds} seconds then make the retry {retry} ");
                    }
                )) // we set a retry when some exception happens, or the policy added in the AddPolicyHandler fails.
                .AddTransientHttpErrorPolicy(build => build.Or<TimeoutRejectedException>().CircuitBreakerAsync(3, TimeSpan.FromSeconds(15),
                    onBreak: (outcome, timespan) =>
                    {
                        var provider = service.BuildServiceProvider();
                        provider.GetService<ILogger<Client<T>>>()?.LogWarning($"The circuit will be open during {timespan.TotalSeconds}");
                    },
                    onReset: () =>
                    {
                        var provider = service.BuildServiceProvider();
                        provider.GetService<ILogger<Client<T>>>()?.LogWarning($"The circuit is closed");
                    }
                ))
                .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(serviceCommunicationSettings.TimeoutSecs));
            return service;
        }
    }
}