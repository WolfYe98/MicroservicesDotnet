using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using Play.Common.Entities;
using Play.Common.Repositories;

namespace Play.Common.MongoDB
{
    public class MongoRepository<T, TKey> : IRepository<T, TKey> where T : IEntity<TKey>// Esto es basicamente un DAO.
    {
        // En mongoDB una coleccion es un grupo de objetos.
        private readonly IMongoCollection<T> dbCollection; //Aqui declaramos una coleccion de mongodb, con readonly hacemos que la variable no pueda cambiar de objeto, es decir un new despues de haber asignado un valor a la variable no es posible, pero si es posible usar add.
        private readonly FilterDefinitionBuilder<T> filterBuilder = Builders<T>.Filter; // esto es para construir filtros para hacer las "queries" de mongoDB.
        public MongoRepository(IMongoDatabase database, string collectionName) // todo lo que hay aqui son accesos a la bd de mongo, esto implica a que tenemos que tener creado una bd de mongo.
        {
            dbCollection = database.GetCollection<T>(collectionName);
        }
        // usaremos async en todos los metodos para mejorar la performance.
        public async Task<IReadOnlyCollection<T>> GetAllAsync() // al ser un Get, no se debe modificar nada, por eso usamos un readonly collection.
        {
            return await dbCollection.Find(filterBuilder.Empty).ToListAsync();
            // con find encontramos la coleccion de datos segun los filtros, como vamos a devolver todos, usamos filterBuilder.Empty, indicando que no hay filtros.
            // con ToListAsync convertimos la colleccion en una lista de forma asincrona.
        }
        public async Task<IReadOnlyCollection<T>> GetAllAsync(Expression<Func<T, bool>> filter)
        {
            return await dbCollection.Find(filter).ToListAsync(); // MongoDB
        }
        public async Task<T> GetAsync(TKey id)
        {
            // FilterDefinition es el filtro que vamos a usar, lo vamos a crear con el filterBuilder.
            // Este filtro es basicamente el where, como no tenemos queries, tenemos que usar filtros en todos los sitios que necesitamos, tanto para el find como para el replace update o delete
            FilterDefinition<T> filtro = filterBuilder.Eq(entity => entity.Id, id); // eq es el metodo que indica que el valor de un campo tiene que coincidir con el segundo parametro.
            return await dbCollection.Find(filtro).FirstOrDefaultAsync(); // queremos encontrar el primer objecto que coincide con el filtro.
        }

        public async Task<T> GetAsync(Expression<Func<T, bool>> filter)
        {
            return await dbCollection.Find(filter).FirstOrDefaultAsync();
        }
        // creamos un item.
        public async Task CreateAsync(T entity) // Si usamos async Task, no es necesario ningun return.
        {
            if (entity == null) // si no se ha recibido ningun item, exception.
                throw new ArgumentNullException(nameof(entity));
            await dbCollection.InsertOneAsync(entity); // esto inserta un objeto en la coleccion
        }
        public async Task UpdateAsync(T entity)
        {
            if (entity == null) // si no se ha recibido ningun item, exception.
                throw new ArgumentNullException(nameof(entity));
            FilterDefinition<T> filtro = filterBuilder.Eq(entity => entity.Id, entity.Id); // usamos este filtro para indicarle al update el where, basicamente esto es un where x=y
            await dbCollection.ReplaceOneAsync(filtro, entity); // con esto reemplazamos un solo objeto que cumple con el filtro por el nuevo entity.
        }

        public async Task RemoveAsync(TKey id)
        {
            FilterDefinition<T> filtro = filterBuilder.Eq(entity => entity.Id, id); // usamos este filtro para indicarle al update el where, basicamente esto es un where x=y
            await dbCollection.DeleteOneAsync(filtro); // borramos un objeto que cumpla con el filtro.
        }
    }
}

