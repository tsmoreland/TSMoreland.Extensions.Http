using System.Net.Http;

namespace TSMoreland.Extensions.Http
{
    public interface IHttpClientRepository : IHttpClientFactory
    {

        bool TryRemoveClient(string name);
    }
}
