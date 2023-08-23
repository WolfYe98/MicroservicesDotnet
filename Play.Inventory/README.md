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

## Synchronous communication:
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
The `AddHttpClient` method injects a `HttpClient` object for the `CatalogClient` object, the `CatalogClient` object should have a constructor which received a `HttpClient` object.\

But all this should be generic, so it should goes to the `Play.Common` package.

If we want to generalize the CatalogClient so we use an interface and the dependency injection, doing it this way:\
```cs
services.AddHttpClient<IClient<T>,Client<T>>(client=>...);
```
The above code basically do this:\
wherever the code found a `IClient<T>`, it creates a `Client<T>` objects and inject a `HttpClient` into the object, it is like basically combine the `AddSingleton<>` and the `AddHttpClient<>` method in one, and this works perfectly while if we try to use the `AddSingleton<>` method and the `AddHttpClient<>` method separately, it fails.


### Timeouts
As the microservices can depends one of another, sometimes when a client make a request to a service, this service can call other service, but the other service can fail or take too long to respond, on that scene, the service will wait to other service and the client will wait to the service, and that can produce a infinite waiting time for the client.\
To avoid those scenes, we have to implement timeouts, when a callee service has reached the time out, the caller service or the client can cancell the request to avoid infinites waitings.\
We have to add the `Microsoft.Extensions.Http.Polly` package for that, then we have to add some code after where we add the HttpClient. So if we are adding the httpClient in the `Play.Commmon` library, we have to modify that library and import it again.\

To add timeouts and retries, we have to add two things after `AddHttpClient<>` method:
1. `AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(NumberOfSeconds))`: this method adds a handler to the `HttpClient` that sets the timeout to the number of `NumberOfSeconds`, and return a `HttpResponseMessage` to the client if the timeout is reached, the `Policy.TimeoutAsync` static method is from `Microsoft.Extensions.Http.Polly` library. **This method is a method of `IHttpClientBuilder`, is not a method of `IServiceCollection` so that is why it has to be after `AddHttpClient` method, because it returns a `IHttpClientBuilder` object.**
2. `AddTransientHttpErrorPolicy(builder=>builder.Or<TimeoutREjectedException>().WaitAndRetryAsync(TotalRetries,retryNumber=>{}))`: this method add a policy to when an Error is thrown in the httpClient, with `builder.Or<TimeoutRejectedException>().WaitAndRetryAsync` policy we are telling to the httpClient that we want to retry after a error. The `Or<TimeoutRejectedException>()` is for combine this method with the `AddPolicyHandler` method, the `Or` method basically is telling which exception should be catched when the timeout defined in the `AddPolicyHandler` method is reached, because the `AddPolicyHandler` method throws a timeout exception because of the `Policy.TimeoutAsync` method, and that exception may not be catched by the `AddTransientHttpErrorPolicy` so we are telling the builder to catched too.\
The `TotalRetries` is the number of retries we want to retry, and the `retryNumber=>{}` lambda function is a function that we will use to set the maximum timeout for each retry. we can use a third parameter `onRetry: (outcome,timespan,retryNumber)=>{}`, this parameter defines additional actions that will be done during a retry call, like put a log message in the log file. The complete method will be something like this:
```cs
services.AddHttpClient<IClient<T>,Client<T>>(client => client.BaseAddress = new Uri(...))
        .AddTransientHttpErrorPolicy(builder=>builder.Or<TimeoutREjectedException>().WaitAndRetryAsync(5, attemp=>TimeSpan.FromSeconds(Math.Pow(2,attemp)),onRetry:(outcome,timespan,attemp)=>{}))
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
```
we are doing the `AddTransientHttpErroPolicy` before the `AddPolicyHandler` so the `AddTransientHttpPolicy` can wrap the `AddPolicyHandler` and catch the error of this last one, as we want to wrap the last policy, that is why in builder we use the `Or` method to catch the possible exceptions thrown by the last policy.
---
### Circuit breaker pattern: how to avoid exhaust the resources of microservices.
#### Resource exhaustion
We the requests number to a microservice is getting bigger, the number of threads of the service that should handeler the requests will increase too, until all the threads are used so the service can not handle more requests, that is what we call a resource exhaustion. When this happens the entire service becomes unavailable, and it causes a lot of trouble in the system. Some scenes that can cause this exhaustion is when an external dependency fails, then every request will try to access to the external dependency and it will wait till it fails, and while the external dependency is failing, more request come and use more resources, as we are waiting too long for an external dependency, the requests will not be responded in a long time and the threads will be busy.\
To avoid that there is the __Circuit breaker pattern__, this pattern works like this:\
We are getting a lot of requests, those requests needs something from an external dependency and that dependency are failing, so we have now a intermediary called __circuit breaker__ and all the requests that want to use the external dependency, will pass throught it, and the __circuit breaker__ will be monitoring the result of each request that have been in it. When the __circuit breaker__ detects that the rate of failure is higher than the configured threashold, it will immediatly stop letting any request to go out and it will fail all the requests, that is what we call **open the circuit**, after this the requests will keep failing during a configured time, during this time we hope the external dependency is correctly working again, after the time the __circuit breaker__ will let some requests to go out to the external dependency and if those requests are success, the circuit will let all the other requests go to the external dependency, that is what we call **close the circuit**. 

To implement this we will add another policy to the httpClients, using again the `AddTransientHttpErrorPolicy()` method:
```cs
services.AddHttpClient<IClient<T>,Client<T>>(client => client.BaseAddress = new Uri(...))
        .AddTransientHttpErrorPolicy(builder=>builder.Or<TimeoutRejectedException>().WaitAndRetryAsync(5, attemp=>TimeSpan.FromSeconds(Math.Pow(2,attemp)),onRetry:(outcome,timespan,attemp)=>{}))
        .AddTransientHttpErrorPolicy(builder=>builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(MaxFailedRequestAllowedUntilOpenCircuit,TimeToKeepTheCircuitOpen))
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
```
As the `WaitAndRetryAsync` method, the `CircuitBreakerAsync` method receives some arguments:
1. The first argument `MaxFailedRequestAllowedUntilOpenCircuit` is the max number of failed requests to reach so the circuit will open.
2. The second argument `TimeToKeepTheCircuitOpen` is the time that the circuit will be open.
3. Additionally it can receive a onBreak and onReset arguments that can do additional things when opens and close the circuit.
---

## Asynchronous communication
Imagin the following scenario:\
We have a service which has an external dependency, and that external dependency has another one, and the another one has other one and go on... We initialy has a 300ms as the response time for our service to the client, but due to the external dependencies, with synchronous calls it add more time to the initial response time, and the total response time could increase too much.\
Another scenario can be that one of the external dependencies fails, and that can cause the entire dependency chain broke. We have keep in mind a really important concept: **Service Level Agreement (SLA)**, the SLA is basically a commitment between the service provider (you) and the client, one part of the SLA could be that the service should be up at the 99.9% of minutes during one month, that means that in a month we can not have more than 44 minutes of down time in our service.\
Imagin now that every dependencies have also a SLA of 99.9%, if one dependency fails, it makes his dependent down its SLA, and the other dependent will decrease its SLA too, making the first of all services, potencially reduces it SLA.

To avoid all this downgrade of SLA, there is the **Asynchronous communication style**, in this style:
1. The client does not wait for response in a timely manner, and depend on how it is set up, there might be no response at all.
2. There usually have a intermediary that is a lightweight **message broker** which has hight availability.

The **message broker** acts like an intermediary, the clients send the message to the broker (remember, a client could be another service) and the broker forwards the messages to receivers as soon as possible, the messages can be received by:
1. A single receiver: which the message acts like a command that the client requests an action on the receiving service. (asynchronous commands)
2. Multiple receivers: the services subscribe to the events published by the client service, this is like when we subscribe a channel in youtube and when the channel upload a video, youtube tells to every suscribers that there is a new video from the channel, the channel would be the client service, the subscribers would be the receiving service and youtube would be the message broker. (publish/subscribe events)

__Imagine now this scenario, every external dependencies now is gone, no service has any external dependency, instead, all the services communicates to the **message broker** and if some dependency fails, there is no problem because the **message broker** will be the one who manage the requests, and this makes the partial failures not affecting on the SLA of other services.__
