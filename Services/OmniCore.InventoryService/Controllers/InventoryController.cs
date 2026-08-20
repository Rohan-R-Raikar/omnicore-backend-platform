using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniCore.InventoryService.Models.DTOs;
using OmniCore.InventoryService.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using OmniCore.InventoryService.Models.Exceptions;

namespace OmniCore.InventoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(
        IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<InventoryResponse>> GetByProductId(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var inventory =
            await _inventoryService.GetByProductIdAsync(
                productId,
                cancellationToken);

        if (inventory is null)
        {
            return NotFound(new
            {
                message = "Inventory not found for this product."
            });
        }

        return Ok(inventory);
    }

    [HttpPut("{productId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InventoryResponse>> Update(
        Guid productId,
        UpdateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var inventory =
            await _inventoryService.UpdateAsync(
                productId,
                request,
                cancellationToken);

        return Ok(inventory);
    }

    [HttpPost("{productId:guid}/reserve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InventoryResponse>> Reserve(
    Guid productId,
    ReserveInventoryRequest request,
    CancellationToken cancellationToken)
    {
        try
        {
            var inventory = await _inventoryService.ReserveAsync(
                productId,
                request.Quantity,
                cancellationToken);

            if (inventory is null)
            {
                return NotFound(new
                {
                    message = "Inventory not found for this product."
                });
            }

            return Ok(inventory);
        }
        catch (InventoryConcurrencyException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPost("{productId:guid}/release")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InventoryResponse>> Release(
    Guid productId,
    ReserveInventoryRequest request,
    CancellationToken cancellationToken)
    {
        try
        {
            var inventory = await _inventoryService.ReleaseAsync(
                productId,
                request.Quantity,
                cancellationToken);

            if (inventory is null)
            {
                return NotFound(new
                {
                    message = "Inventory not found for this product."
                });
            }

            return Ok(inventory);
        }
        catch (InventoryConcurrencyException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }
}