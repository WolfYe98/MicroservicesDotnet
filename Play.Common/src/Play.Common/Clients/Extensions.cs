using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Play.Common.Settings;

namespace Play.Common.Clients
{
    public static class Extensions
    {
        public static IServiceCollection AddServiceCommunicationClient<T>(this IServiceCollection service)
        {
            service.AddSingleton<IClient<T>>(provider => // we have to add the correct httpClient to the service with the correct BaseAddress.
            {
                var configuration = provider.GetService<IConfiguration>();
                var serviceCommunicationSettings = configuration.GetSection(nameof(ServiceCommunicationSettings)).Get<ServiceCommunicationSettings>(); // the address will be retrieved from the appsetting.json file.
                HttpClient httpClient = new HttpClient
                {
                    BaseAddress = serviceCommunicationSettings.CommunicationUri
                };
                return new Client<T>(httpClient);
            });
            return service;
        }
    }
}