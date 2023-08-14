# Microservice Catalog
This is the first service of the course [Freecodecamp .NET Microservices](https://www.youtube.com/watch?v=CqCDOosvZIk&t=4497s).

## Steps to create the microservice.
### First step: create the service folder structure.
For create the service, we have used the command:

```dotnet new webapi -n ServiceName```

the `-n` option is for indicate which name should have the service.

After execute the command, we have remove the default controller.

### Second step: create the DTOs
We created first the DTOs that we will use to transfer datas to other services or to the client, in this case we use `record` type as the DTOs type.

`record` type is a type of data that are inmutables, it means that each property of the record can not be modified once it has been created. So it is perfect for data transfer.

_NOTE:_ _With unmutable I mean that is unmutable for the value of the property, I mean, if you have a property that is a int type, you can not change it value since it is a primitive data type, but if you have an object, the only thing you can not do is to reassign the property to other object, but you can modify the object. For Example:_

```cs
public record Person(string Name, string Surname, List<string> PhoneNumbers);
public void SetPerson(){
    Person p = new("Toby","McDonald",new List<string>(){"1234","12345"});
    p.Name = "Hola MUNDO"; // INVALID 
    p.PhoneNumbers.Add("4321"); // VALID
    p.PhoneNumbers = new List<string>(); // INVALID.
}
```
So, using `record` type help us to create DTO without the need to declare a long class.

### Third step: create a controller
We have to create all controllers under the Controllers directory, and every controller should inherit from `ControllerBase` and should have the attribute `[ApiController]` to indicate that is a webapi controller. 

It also need the `[Route("routename")]` attribute to specify the access route to the controller, for example:
```cs
[ApiController]
[Route("myController")]
public class MyController:ControllerBase{

}
```
to access to each action of this controller we have to use the route /myController.

For each action we should use the correct http verb, as `HttpGet`,`HttpPost`,`HttpPut`,`HttpDelete`.

And we can pass arguments to indicate which mandatory field should be pass to the controller, for example:
```cs
[HttpGet("{id}")]
public Item GetItem(int id){
    ...
}
```
the code above indicates that we have to add an id in the route to get the items, as `/myController/1`.

Accessing to `/myController` it can access to a lot of Actions, so that is why in the request it should always send the HttpVerb to access. 

_NOTE: as every action in the controller that doesn't have any argument in the http verb can be called when you access to /myController route, you should always specify which http verb to use in your request._


### Fourth step: create db access
In this case, the db access objects is called Repositories, it is basically a DAO, a layer between application logic and the data access logic.

For the Repositories, we should not use the DTOs, as the DTO are only for transfer data between client and server, in this case we have to create Entities, which are basically a class with only properties with get and set.

```cs
public class Item{
    public string Name{get;set;}
    public string Description{get;set;}
}
```

We are using MongoDB in this course, to add MongoDB to our project, we can use the command ```dotnet add package MongoDB.Driver```.

After that we create our first Repository:
```cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Play.Catalog.Service.Entities;

namespace Play.Catalog.Service.Repositories
{
    public class ItemsRepository // Esto es basicamente un DAO.
    {
        private const string collectionName = "items"; // En mongoDB una coleccion representa un grupo de objetos
        private readonly IMongoCollection<Item> dbCollection; //Aqui declaramos una coleccion de mongodb, con readonly hacemos que la variable no pueda cambiar de objeto, es decir un new despues de haber asignado un valor a la variable no es posible, pero si es posible usar add.
        private readonly FilterDefinitionBuilder<Item> filterBuilder = Builders<Item>.Filter; // esto es para construir filtros para hacer las "queries" de mongoDB.
        public ItemsRepository() // todo lo que hay aqui son accesos a la bd de mongo, esto implica a que tenemos que tener creado una bd de mongo.
        {
            var mongoClient = new MongoClient("mongodb://localhost:27017"); // creamos el cliente de mongodb para luego construir la coleccion de mongodb. El parametro es un connection string, debe ir a un archivo de configuracion
            var database = mongoClient.GetDatabase("Catalog"); // elegimos la base de datos que queremos usar.
            dbCollection = database.GetCollection<Item>(collectionName);
        }
        // usaremos async en todos los metodos para mejorar la performance.
        public async Task<IReadOnlyCollection<Item>> GetAllAsync() // al ser un Get, no se debe modificar nada, por eso usamos un readonly collection.
        {
            return await dbCollection.Find(filterBuilder.Empty).ToListAsync();
            // con find encontramos la coleccion de datos segun los filtros, como vamos a devolver todos, usamos filterBuilder.Empty, indicando que no hay filtros.
            // con ToListAsync convertimos la colleccion en una lista de forma asincrona.
        }
        public async Task<Item> GetAsync(Guid id)
        {
            // FilterDefinition es el filtro que vamos a usar, lo vamos a crear con el filterBuilder.
            // Este filtro es basicamente el where, como no tenemos queries, tenemos que usar filtros en todos los sitios que necesitamos, tanto para el find como para el replace update o delete
            FilterDefinition<Item> filtro = filterBuilder.Eq(entity => entity.Id, id); // eq es el metodo que indica que el valor de un campo tiene que coincidir con el segundo parametro.
            return await dbCollection.Find(filtro).FirstOrDefaultAsync(); // queremos encontrar el primer objecto que coincide con el filtro.
        }
        // creamos un item.
        public async Task CreateItem(Item entity) // Si usamos async Task, no es necesario ningun return.
        {
            if (entity == null) // si no se ha recibido ningun item, exception.
                throw new ArgumentNullException(nameof(entity));
            await dbCollection.InsertOneAsync(entity); // esto inserta un objeto en la coleccion
        }
        public async Task UpdateAsync(Item entity)
        {
            if (entity == null) // si no se ha recibido ningun item, exception.
                throw new ArgumentNullException(nameof(entity));
            FilterDefinition<Item> filtro = filterBuilder.Eq(entity => entity.Id, entity.Id); // usamos este filtro para indicarle al update el where, basicamente esto es un where x=y
            await dbCollection.ReplaceOneAsync(filtro, entity); // con esto reemplazamos un solo objeto que cumple con el filtro por el nuevo entity.
        }

        public async Task RemoveAsync(Guid id)
        {
            FilterDefinition<Item> filtro = filterBuilder.Eq(entity => entity.Id, id); // usamos este filtro para indicarle al update el where, basicamente esto es un where x=y
            await dbCollection.DeleteOneAsync(filtro); // borramos un objeto que cumpla con el filtro.
        }
    }
}
```
See the comments in code to understand how MongoDB works.

### Fifth step: create extension class to map objects to DTOs
As we said before, DTOs objects should be the only type of object that are transfer from client to server and vice versa.

So after getting, updating, inserting or deleting data, if we want to send back an object, we have to send back a DTO, that is why we should have classes that can help us to map an entity object to a DTO object.

This classes are `Extension` classes, this classes usually add other functionalities to other classes.

For example:
```cs
public static class Extensions{

    public static ItemDto AsDto(this Item item)
    {
        return new ItemDto(item.Id, item.Name, item.Description, item.Price, item.CreatedDate);
    }
}
```
The class above extends the class Item (using `this Item item`), so now each Item object has a new method that doesn't receive any arguments called AsDto and it returns an ItemDto object.
For example:
```cs
[HttpGet("{id}")]
public ActionResult<ItemDto> GetById(int id){
    Item item = ... // database getting the item by id.
    if(item == null){
        return NotFound();
    }
    return item.AsDto();
}
```
In the above code, we can see that we can return an ItemDto object from an Item object just by using the Extension method `AsDto()`.


### Sixth step: improving time with async/await
As the asynchronouns programming will potentially improves our microservices, we will use it here in our microservice.

To use async/await we have to use Task<>.
A simple use of async/await in a Repository could be:
```cs
public async Task CreateItem(Item entity) 
{
    if (entity == null) 
        throw new ArgumentNullException(nameof(entity));
    await dbCollection.InsertOneAsync(entity);
}
 public async Task<Item> GetAsync(Guid id)
{
    FilterDefinition<Item> filtro = filterBuilder.Eq(entity => entity.id, id);
    return await dbCollection.Find(filtro).FirstOrDefaultAsync(); 
}
```
And a simple use of async/await in an action could be:
```cs
[HttpGet("{id}")] // GET /items/{id} f.e.: /items/1234
public async Task<ActionResult<ItemDto>> GetById(Guid id)
{
    Item item = await itemsRepository.GetAsync(id);
    if (item == null)
        return NotFound();
    return item.AsDto();
}
[HttpPost] 
public async Task<ActionResult<ItemDto>> Post(CreateItemDto newItem)
{
    if (!ModelState.IsValid)
    {
        return BadRequest();
    }
    Item item = new Item()
    {
        Name = newItem.Name,
        Description = newItem.Description,
        Price = newItem.Price,
        CreatedDate = DateTimeOffset.UtcNow
    };
    await itemsRepository.CreateItem(item);
    return CreatedAtAction(nameof(GetById), new { id = item.Id }, item.AsDto());
}
```
We still have to make a little change if we want the actions use async/await

In .NET, by default all actions supress the async suffix when they are called, and that produced an error if we want to use async/await in the actions. For resolve that problerm, we have to change the method `ConfigureServices` of the `Startup.cs` class by adding an option that set the SupressAsyncSuffixInActionNames to false:
```cs
public void ConfigureServices(IServiceCollection services)
{

    services.AddControllers(options =>
    {
        options.SuppressAsyncSuffixInActionNames = false;
    });
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Catalog.Service", Version = "v1" });
    });
}
```


## Steps to create a MongoDB database using Docker.
First we have to run docker.
We can run docker using a specific image using this command:
```
docker run -d --rm --name mongo -p 27017:27017 -v mongodbdata:/data/db mongo
```

`-d` detach, indicates that the container will be executed in background (so the terminal will not showing the docker status).

`--rm` this option removes the container when it finished the execution.

`--name name` the name of our container.

`-p externalPort:internalPort` set the external and the internal port that the container will be using.
The port should be used lately as the port of the connection string for our app.


**`-v volumeName:path` specify the volume that will be mounted over the specific path of the container. With this option, any time that the container try to store data in his directory /data/db (default path where mongoDB stores data), it will actually store it in our local docker volume `mongodbdata`.**

**We can give the volume anyname we want.**

With the `-v` option we can ensure that all the data inserted in the database server container, is stored in our local machine too. We have mounted a local volume (is like a directory) to a specific directory of the container (/data/db in this case). It works like `mount` command of linux, you create a directory in a path and mount an usb device on that directory, then if you store something in that directory the mounted usb device has the same thing. 

We will be able to check the mounted volume in the Docker app -> Volumes tab.

`mongo` this last mongo is the name of the image that we want to use.

After execute this docker command, we can check if the container is actually created with: 
`docker ps`

We can connect to our MongoDB database using the vscode MongoDB extension.

If we want mongoDB shows special values as readable string (like datetime as string instead of a long), we have to add a serializer in the `Startup.cs` -> `ConfigureServices` method.

```cs
BsonSerializer.RegisterSerializer(new DataTypeNameSerializer(BsonType.String));
```
Where *DataTypeName* should be replaced as the datatype that we want to serialize (DateTimeOffset, Guid etc...).


## Configuration Sources
We should avoid the bad practice of hard coding all the sensitive data (like the database host, database usename and password etc...) in the code, instead we should use a configuration file to store all those sentitive data.

In Asp.NET core we have the `appsettings.json` file, in this file we should store al the sensitive data.

The data of this file and others like local secrets, cluod, enviroment variables are automatically loaded into the configuration system during the startup, and the startup is configured in the `Program.cs` file.

We can add settings on our `appsettings.json` file by just adding fields.\
For example:
```json
"MongoDbSettings": {
    "Host": "localhost",
    "Port": "27017"
},
"DatabasesSettings": {
    "DatabaseName": "Catalog"
}
```
_NOTE: the code above is some section that we added in the appsettings.json file_

We can create classes to use the settings defined in the `appsettings.json` file and then added to the ConfigureService method on `Startup.cs`.\
For example:
```cs
public class DatabasesSettings
{
    public string DatabaseName { get; init; }
}
```
As we can see, the class is just like a DTO class, this is for a more simply use after.

We can create a serviceSetting object by retrieving the values defined in the `appsettings.json` file by doing this:
```cs
public class Startup{
    public DatabasesSettings s; //ServiceSettings is a class created by us and the name match with a field in the appsettings.json file.
    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        s = Configuration.GetSection(nameof(DatabasesSettings)).Get<DatabasesSettings>(); // we retrieve the service settings object with the values in the appsettings.json file, that is why we have to declare the class name equal than the section name in appsettings.json file.
    }
}
```
Now the `s` object properties will have the value defined in the `appsettings.json` file.


## Dependency injection
Following the Dependency Inverse Principle, we should use more interfaces to decouple the dependencies of classes.

So that is, if we have a repository class, we should have a interface with all the methods that we want to use in other classes, the other classes instead of using the repository class, will use the interface and in the constructor, we will receive an object that implements that interface.

This sound easy when we are using this principle with our own classes, because we write the code and then we decide in code which class should be injected to the new object, **but what happens if you want to use dependency injection in a Controller class?** You can declare the constructor of the controller class and say that you want to receive an object of some interface as the constructor's argument but, when the controller object is created? **Because it is something of .NET and not yours**, so to tell .NET hey use these classes that implement x interface as a dependency injection, we have to use a ServiceProvider and register our dependency classes.\
For example:

Controller class
```cs
public class ItemsController : ControllerBase
{
    private readonly IItemsRepository itemsRepository;
    public ItemsController(IItemsRepository itemsRepository)
    {
        this.itemsRepository = itemsRepository;
    }
}
```
We have implemented dependency injection by using a parameter in the constructor to assign a object to `itemsRepository` variable, but how can .NET know which class should use?

We can do the same for DAOs:
```cs
public ItemsRepository(IMongoDatabase database)
{
    dbCollection = database.GetCollection<Item>(collectionName);
}
```
Then we should added it in the `Startup.cs` file.\
For the repository class, we can register our service this way:
```cs
 services.AddSingleton(serviceProvider =>
{
    var mongodbsettings = Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
    var mongoClient = new MongoClient(mongodbsettings.ConnectionString);
    return mongoClient.GetDatabase(serviceSettings.ServiceName);
}); // add a singleton object to he service
```
We add a singleton because we don't want another conection to the database, we get the settings value from the `appsettings.json` file using `Configuration` just as we explain before and then, we get the conection of the `MongoClient` using the ConnectionString property defined in the `MongoDbSettings` class (in the `appsetting.json` file there are not that property).\
Finally we return the database so the database is the singleton register in our app service provider.\
With this code, the mongoDB database is added to the service provider and it can start to used it in every places that use `IMongoDatabase` interface.\

We have to register now the `IItemRepository` interface, to achive this, we can simply add this line of code:
```cs
services.AddSingleton<IItemsRepository, ItemsRepository>(); // we tell the service provider that where request IItemsRepository we will return a ItemsRepository object.
```

Above are two ways we add an object to use in dependency injection by .NET, they are different because one we have to use some settings and the other we dont have to.

## Postman
We can test our app using postman instead of swagger, for simplify the postman enviroment, we can go first to swagger, click on the link that say "/swagger/v1/swagger.json" or something like that, and then we copy the open url from the browser (something like: https://localhost:5001/swagger/v1/swagger.json),
then we can go to postman, click on import o + in the left side, we paste the link and then a entire enviroment will be imported to our postman, so we don't have to configure every http action.\

We have also define some parameter needed to test like {{baseUrl}}, for that, we can click in the ... of our imported collection (Play.Catalog.Service in this case) and we click on edit, then we go to the Variables tab and we change the initialvalue and the current value to https://localhost:5001.\
The initial value is something the variable have at the very beginning of the app, the current value may be changed on some request, like a token can be null at the beginning but in some auth action it changes its value to some token value, so that new value should be stored in the current value.\
Or we can create a enviroment and store the variables in the enviroment.


## Reusing common code
As we just created our first microservice, we should keep in mind that when we create another microservice or microservices, we may have to use some same code, as the Repositories, Settings, Service brocker or instrumentation codes, and that violates the DRY (Don't Repeat Yourself) principle, so we have to implement a way to reuse the code that we already have.\
We may think that the best way is referencing to the microservice which have the code we need, but that is not a good approach since the microservices should be independent between them.\
To resolve this problem, we can create a common library where we store all the common codes and then the microservices can:
1. Direct reference to the library: this is not a good approach neither, because some time we just want to work on our microservice and not in other libraries and each microservice and library may have their own repository. And this approach also breaks a little the independency of the microservices.
2. Using **NUGET**: Nuget is the package manager of dotnet, we can use `dotnet pack` command to bundle all the output files a nuget package. A nuget package is just a zip with .pkg extension that contains all the files that are to be shared to other projects.

Nuget packages can be hosted in the local machine or in a cloud-based enviroment, this approach help us to have the common code maintained in a single place and create new microservices time will be reduced.\
Some common code can be like the Repositories, there is several same method for every repository and that is get, update, insert and delete, so we could have a single class that receives a generic type and then the repository could go to a package.

For example:
```cs
public interface IRepository<T> where T : IEntity
{
    Task CreateAsync(T entity);
    Task<IReadOnlyCollection<T>> GetAllAsync();
    Task<T> GetAsync(Guid id);
    Task UpdateAsync(T entity);
    Task RemoveAsync(Guid id);
}
```
We can have a generic type in the interface and say that the generic type should be a class that implements `IEntity`.\
And the repository will be modified to something like:
```cs
 public class MongoRepository<T> : IRepository<T> where T : IEntity// Esto es basicamente un DAO.
{
    private readonly IMongoCollection<T> dbCollection;
    private readonly FilterDefinitionBuilder<T> filterBuilder = Builders<T>.Filter;
    public MongoRepository(IMongoDatabase database, string collectionName) 
    {
        dbCollection = database.GetCollection<T>(collectionName);
    } 
    public async Task<T> GetAsync(Guid id)
    {
        FilterDefinition<T> filtro = filterBuilder.Eq(entity => entity.Id, id);
        return await dbCollection.Find(filtro).FirstOrDefaultAsync();
    }
}
```
As we can see, now we have a MongoRepository for CRUD operations to every collection we want.\
We have also changed the registration of the repository.
```cs
//Before
services.AddSingleton<IItemsRepository, ItemsRepository>();
//After
services.AddSingleton<IRepository<Item>>(serviceProvider =>
{
    var db = serviceProvider.GetService<IMongoDatabase>();
    return new MongoRepository<Item>(db, "items");
}); // we tell the service provider that where request IItemsRepository we will return a ItemsRepository object.
```
Before, we just add a singleton that maps `IItemRepository` to `ItemRepository`, now, as the repository is generic, we have to add it using the delegate form, by creating and returning an object of type `MongoRepository<Item>` using the db in the serviceProveder, and the `collectionName` we assigned it to the constructor. (In the before example we assigned it just by a constant in the ItemRepository, now we use the constructor).

**To create a new library of common code we have to create first a new Project Directory and here is the [README about how to create a library in .NET](../../../Play.Common/README.md)**

After created a package, we want to use it in our project, we can used it by doing these steps:\
1. We have to tell to nuget where can nuget find the packages, by default it always goes to the Nuget cloud. To add a new source of package we have to execute this command:
```
dotnet nuget add source PathToPackagesDirectoryOrURL -n SourceNameForNuget
```
`PathToPackagesDirectoryOrURL` can be a url or a local path.\
`SourceNameForNuget` is just a name that Nuget will asociate to the path.\
Once we execute the command, we have a new source of packages included in our nuget.\
2. Now we have to delete all these references that are already added in the package from our project, in our case it will be the MongoDB.Driver package, we can just simply remove it from .csproj file.\
3. We can now add the package by using this command:
```
dotnet add package PackageName
```
`PackageName` is the name of our package, in this case it is Play.Common.\
Now we can Remove all the files that we will not be using, like IEntity, or the settings file.

## Simplify the Startup class.
We can create another Extension class for simplify the startup class, this new extension class as we want to extends IServiceCollection with methods that register Repositories classes, we have to create the `Extensions.cs` class in the Repositories namespace.\
After we create the `Extensions` class, we have to add the mongoDB service and the repositories services:
```cs
public static class Extensions
{
    public static IServiceCollection AddMongo(this IServiceCollection services)
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.BsonType.String));
        BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(MongoDB.Bson.BsonType.String));

        services.AddSingleton(provider =>
        {
            var configuration = provider.GetService<IConfiguration>(); // get the configuration service
            var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>(); // get the serviceSettings section.
            var mongodbConfiguration = configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
            var mongoClient = new MongoClient(mongodbConfiguration.ConnectionString);
            return mongoClient.GetDatabase(serviceSettings.ServiceName);
        });// add a singleton object of mongo db so wherever is getting IMongoClient, is getting MongoClient object defined here

        return services;
    }
    public static IServiceCollection AddMongoRepository<T>(this IServiceCollection services, string collectionName) where T : IEntity
    {
        //Now we want to add a singleton for the IRepository interface, this one is for items
        services.AddSingleton<IRepository<T>>(provider =>
        {
            var db = provider.GetService<IMongoDatabase>(); // we retrieve the db from services that we added before.
            return new MongoRepository<T>(db, collectionName);
        });
        return services;
    }
}
```
As we can see in the code above, the `AddMongo()` method will add the mongoDB database object to the service provider, and the `AddMongoRepository(collectionName)` will add MongoRepository in a generic way, we use where T:IEntity because the interface `IRepository` receive only object of classes that implements the `IEntity` interface.\
If
```cs 
where T : IEntity
```
is not in the method, the line 
```cs
services.AddSingleton<IRepository<T>>
```
will throw an Error.

## Docker compose
At this moment, we are just executing one container of mongodb, by using just one command, but what happens when we want to run multiple containers? Do we have to execute multiple commands and remember all its options? The answer is no, you can simplify all that process with **docker compose**.

Docker compose also help us to resolve the problem of dependencies between containers and if a container needs to talk to other, docker compose help us to do it's comunication.

Docker compose is a tool for defining and running multi-container docker applications.

We define a `docker-compose.yml` file, this file will include the definition of container to use in each case, including enviroment variables, ports and even dependencies between them. Then we have just use the command `docker-compose up` to start all the containers in the right order with the right configuration. Also docker compose provides a network that let the containers talks to each other.

**To learn about docker-compose.yml [click here](../../../Play.Infra/README.md)**

## Run the project
You can clone the project and use the commands ```dotnet run``` or ```dotnet watch run``` to use hotReload.

