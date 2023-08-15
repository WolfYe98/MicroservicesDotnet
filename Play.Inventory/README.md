# Second Microservice
As we just created the first Microservice, we have created now the second one, as in the first one but faster.

## Step 1: create the Dtos.
We create first the Dtos which are the data that we will receive from/send to a client.

## Step 2: create the Entity.
After create the Dtos we have to create the entity that we need to store in the DB, in this case is the `InventoryItem` entity.

## Step 3: create the controller.
Now we have to create the controller, as we have added our `Play.Common` package, we can forget about create some repositories (DAOs) because the `MongoRepository<T,TKey>` will do everything for us (get, insert, update, delete).

## Step 4: define the configuration file.
We have to define now the mandatory settings in the `appsettings.json` file, like the `ServiceSettings` or the `MongoDBSettings`.

## Step 5: register the Mongo Dependency and the Repository Dependency
Now we have to go to the `Startup` class and register the `MongoDB` and the `MongoRepository` dependencies with its corresponding class as T and TKey.\
We also don't have to worry about how to include those dependency because its methods `AddMongo` and `AddMongoRepository<T,TKey>` are already defined in the `Play.Common` package.

## Step 6: communication between microservices.
There is two ways to communicate from a microservice to other:
1. Synchronous: client sends a request and waits for a response from the service.
2. Asynchronouns: client sends a request to the service but the response, if any, is not sent back immediately.

### Synchronous communication:
* The client sends a request and waits for a response from the service.
* The client cannot proceed without the response.
* The client thread may use a blocking or non-blocking implementation (callback), in the non-clibking implementation, the client offers to the service a callback function that the service call when the response is ready, using this approach doesn't block the client thread, even it is still waiting for the response.
* REST + HTTP protocol, is the traditional approach, `swagger` or `Postman` uses this approach.
* gRPC new protocol, is popular now for internal inter-service communication, is a binary message based protocol in which clients and servers exchange messages in the protocol buffers format. It supports http2 and is more efficient that REST, but not all clients support http2, that is why gRPC is more used for internal services communications.

To implement this, we have to follow this steps:
1. We have to create the DTO for the Items, which are objects that we will receive back from the Catalog service, and we just take the fields we need.
2. We have to create Clients to make the communication, so we have to make classes for this.\
To create a client we will be using the `HttpClient` class, we can use dependency injection to assign a client and then, we have just call the route we want, for example:
```cs
namespace Play.Inventory.Service.Clients
{
    public class CatalogClient
    {
        private readonly HttpClient httpClient;
        public CatalogClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
        public async Task<IReadOnlyCollection<CatalogItemDto>> GetCatalogItemsAsync()
        {
            var items = await httpClient.GetFromJsonAsync<IReadOnlyCollection<CatalogItemDto>>("/items");
            return items;
        }
    }
}
```
Then we can use this client in the `Controller`, by using again, dependency injection:
```cs
[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private readonly IRepository<InventoryItem, Guid> repository;
    private readonly CatalogClient catalogClient;
    public ItemsController(IRepository<InventoryItem, Guid> repository, CatalogClient catalogClient)
    {
        this.repository = repository;
        this.catalogClient = catalogClient;
    }
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAllAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest();
        var catalogItems = await catalogClient.GetCatalogItemsAsync();
        var userItems = await repository.GetAllAsync(item => item.UserId == userId);
        var dtosReturn = userItems.Select(x =>
        {
            var i = catalogItems.Single(y => y.Id == x.ItemId);
            return x.AsInventoryItemDto(i.Name, i.Description);
        });
        return Ok(dtosReturn);
    }
}
```
Now we have the client we can use the client to retrieve data from other services, just like how `GetAllAsync` is doing.
Now we have to register our catalog client:
```cs
services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:5001");
});
```
but all this should be generic, so it should goes to the `Play.Common` package.