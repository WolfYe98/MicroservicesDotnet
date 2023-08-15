using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Amazon.Runtime;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Validations;
using Play.Common.Repositories;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;
using Play.Common.Clients;
namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem, Guid> repository;
        private readonly IClient<CatalogItemDto> catalogClient;
        public ItemsController(IRepository<InventoryItem, Guid> repository, IClient<CatalogItemDto> catalogClient)
        {
            this.repository = repository;
            this.catalogClient = catalogClient;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAllAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return BadRequest();
            var catalogItems = await catalogClient.GetElementsAsync("items");
            var userItems = await repository.GetAllAsync(item => item.UserId == userId);
            var dtosReturn = userItems.Select(x =>
            {
                var i = catalogItems.Single(y => y.Id == x.ItemId);
                return x.AsInventoryItemDto(i.Name, i.Description);
            });
            return Ok(dtosReturn);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync(GrantItemsDto dto)
        {
            if (dto == null)
                return BadRequest();
            var existsItem = await repository.GetAsync(item => item.UserId == dto.UserId && item.ItemId == dto.ItemID);
            var item = new InventoryItem()
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                ItemId = dto.ItemID,
                Quantity = dto.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
            };
            if (existsItem == null)
            {
                await repository.CreateAsync(item);
            }
            else
            {
                existsItem.Quantity += item.Quantity;
                await repository.UpdateAsync(existsItem);
            }
            return Ok();
        }

    }
}