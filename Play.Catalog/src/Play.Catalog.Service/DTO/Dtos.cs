using System;
using System.ComponentModel.DataAnnotations;

namespace Play.Catalog.Service.Dtos
{ // Records are basically DTOs that will only be sended, in records, values of the fields can not be changed, except if is a list or array, with case we can not add or remove but yes modify.
    // we use record so we dont have to declare classes with only attributes, if we want a mutable value, we can do the same as we do for a class, public record Record(property1,...,propertyN){ public|private|protected|internal datatype propertyName {get;set;}}
    // we can create a copy of a inmutable record object with the keyword 'with'
    // the ToString method is implicitly implemented.
    public record ItemDto(Guid Id, string Name, string Description, decimal Price, DateTimeOffset CreatedDate);
    public record CreateItemDto([Required] string Name, [Required] string Description, [Range(0, 1000)] decimal Price); // [Required] indicates the name is required, if null empty received, ModelState is not valid, [Range(x,y)] range of values accepted
    public record UpdateItemDto([Required] string Name, [Required] string Description, decimal Price);
}