using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Service.Dtos;
using System.Linq;
using System.Threading.Tasks;
using Play.Catalog.Service.Extensions;
using Play.Catalog.Service.Entities;
using Play.Common.Repositories;

namespace Play.Catalog.Service.Controllers
{
    [ApiController] // this is necesary for every webapi controller.
    [Route("items")] // This route indicate that every action in this controller will be accesible using the route /items, like localhost:5001/items/action
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item, Guid> itemsRepository;
        private static int requestCounter = 0;
        public ItemsController(IRepository<Item, Guid> itemsRepository)
        {
            this.itemsRepository = itemsRepository;
        }
        [HttpGet] // each action should use the correct http operation
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            requestCounter++;
            Console.WriteLine($"Request {requestCounter}: Starting...");
            if (requestCounter <= 2)
            {
                Console.WriteLine($"Request {requestCounter} delay...");
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
            if (requestCounter <= 5)
            {
                Console.WriteLine($"Request {requestCounter} 500 (Internal server error)...");
                return StatusCode(500);
            }
            return Ok((await itemsRepository.GetAllAsync()).Select(x => x.AsDto()));
        }

        [HttpGet("{id}")] // GET /items/{id} f.e.: /items/1234
        public async Task<ActionResult<ItemDto>> GetById(Guid id)
        {
            Item item = await itemsRepository.GetAsync(id);
            if (item == null)
                return NotFound();
            return item.AsDto();
        }
        [HttpPost] // create a new item, the route is /items but it has a body in the request.
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
            await itemsRepository.CreateAsync(item);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item.AsDto());
            // this method name is a little bit strange, but it does this: 
            // return a 201 (created) response to the client sending back the created item using the method GetById
            //  the second argument is the parameter that the method GetById ask for.
            //  and the third value is the value that will be returned to the client.
            // routeValues is always the values that the method accepts as parameter.
            // value in this action will be the value returned to the client, it can be the new item, or others.
            // We can use CreateAtAction with Get method too, CreatedAtAction(nameof(Get),items), this return all the items.
        }
        [HttpPut("{id}")] // we indicate that it is necessary a id in the routes.
        public async Task<IActionResult> Put(Guid id, UpdateItemDto updateItem) // we use here IActionResult because we dont send back a object, we just send a response ok or failure back because usually that is what put does 
        {
            Item item = await itemsRepository.GetAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            item.Name = updateItem.Name;
            item.Description = updateItem.Description;
            item.Price = updateItem.Price;
            await itemsRepository.UpdateAsync(item);
            return NoContent();
        }
        [HttpDelete("{id}")] // delete by id
        public async Task<IActionResult> DeleteById(Guid id)
        {
            Item item = await itemsRepository.GetAsync(id);
            if (item == null)
                return NotFound();
            await itemsRepository.RemoveAsync(id);
            return NoContent();
        }
    }
}