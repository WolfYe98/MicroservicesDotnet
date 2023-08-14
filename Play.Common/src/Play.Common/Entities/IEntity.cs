using System;

namespace Play.Common.Entities
{
    public interface IEntity<T>
    {
        public T Id { get; set; }
    }
}