using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Play.Common.Clients
{
    public class Client<T> : IClient<T>
    {
        private readonly HttpClient httpClient;
        public Client(HttpClient httpClient) // we are using dependency injection, the baseAddress is defined when it is added.
        {
            this.httpClient = httpClient;
        }
        public async Task<IEnumerable<T>> GetElementsAsync(string route)
        {
            var res = await httpClient.GetFromJsonAsync<IEnumerable<T>>(route); // Get and return a object from the json that is returned by the route.
            return res;
        }
    }
}