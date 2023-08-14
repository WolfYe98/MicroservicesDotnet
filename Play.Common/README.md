# Common code of the microservices goes here.

## First Step: create a class library
To do that we have to run this command in the `src` directory:
```
dotnet new classlib -n Play.Common
```

We have to add three packages to the proyect:
```
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Binder
dotnet add package Microsoft.Extensions.DependencyInjection
```

And as we will use MongoDb in this project, we have to add:
```
dotnet add package MongoDB.Driver
```


## Second step: add new functionalities to Repository
We want to add a new function to the repository, we want to let the `GetAsync` and `GetAllAsync` methods can do a filter like `FirstOrDefault(x=>x...)`, for that we have to add this `Expression` as the method parameter:
```cs
public Task<IReadOnlyCollection<T>> GetAllAsync(Expression<Func<T,bool>> filter);
public Task<T> GetAsync(Expression<Func<T,bool>> filter);
```
With the type `Expression` we indicate that the method will receive a lambda expression as the filter, and with `Func<T,bool>` we indicate that the lambda expression should be a function that receive T as argument and return bool.

The difference about using `Expression` and `Func` is that `Func` is just a pointer to a method, but `Expression` has a entire tree data structure.\

### When we have to use Func<> and when we have to use Expression<Func<>>?
If you want to receive a method as parameter because you will execute it in your code at some moment, you should use `Func`, using `Expression` allows you to look inside of an expression tree, like linq, linq never execute your lambda expression, it just get the expression and converts it to SQL Statement.\
The runtime engine can not "look" inside of a `Func`, it can only execute it but the runtime engine can look inside of an `Expression`.

With `Expression<Func<T, bool>> filter` we can make filters by the expression instead of using Id as the only filter.\
By default, `IMongoCollection.Find()` method has a override that accepts by default an expression as argument.

## Final step: create the package
We can use the command
```
dotnet pack -o ..\..\..\packages\
```

`-o outputDirectory` path to the output directory where the output should be stored.\
