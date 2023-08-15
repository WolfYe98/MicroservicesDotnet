using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Play.Common.Clients
{
    public interface IClient<T>
    {
        public Task<IEnumerable<T>> GetElementsAsync(string route);
    }
}