using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniCore.OrderService.Models.DTOs;
using OmniCore.OrderService.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
	private readonly IOrderService _orderService;

	public OrdersController(IOrderService orderService)
	{
		_orderService = orderService;
	}

	[HttpPost("{id:guid}/cancel")]
	public async Task<ActionResult<OrderResponse>> Cancel(
	Guid id,
	CancellationToken cancellationToken)
	{
		var userId = GetCurrentUserId();
		var isAdmin = User.IsInRole("Admin");

		var authorizationHeader =
			Request.Headers.Authorization.ToString();

		try
		{
			var order = await _orderService.CancelAsync(
				id,
				userId,
				isAdmin,
				authorizationHeader,
				cancellationToken);

			if (order is null)
			{
				return NotFound(new
				{
					message = "Order not found."
				});
			}

			return Ok(order);
		}
		catch (InvalidOperationException exception)
		{
			return Conflict(new
			{
				message = exception.Message
			});
		}
		catch (HttpRequestException exception)
		{
			return StatusCode(
				StatusCodes.Status502BadGateway,
				new
				{
					message =
						"A dependent service failed while cancelling the order.",
					detail = exception.Message
				});
		}
	}

	[HttpPut("{id:guid}/status")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<OrderResponse>> UpdateStatus(
	Guid id,
	UpdateOrderStatusRequest request,
	CancellationToken cancellationToken)
	{
		try
		{
			var order = await _orderService.UpdateStatusAsync(
				id,
				request.Status,
				cancellationToken);

			if (order is null)
			{
				return NotFound(new
				{
					message = "Order not found."
				});
			}

			return Ok(order);
		}
		catch (InvalidOperationException exception)
		{
			return Conflict(new
			{
				message = exception.Message
			});
		}
	}

	[HttpPost]
	public async Task<ActionResult<OrderResponse>> Create(
		CreateOrderRequest request,
		CancellationToken cancellationToken)
	{
		var userId = GetCurrentUserId();

		var authorizationHeader =
			Request.Headers.Authorization.ToString();

		try
		{
			var order = await _orderService.CreateAsync(
				userId,
				request,
				authorizationHeader,
				cancellationToken);

			return CreatedAtAction(
				nameof(GetById),
				new { id = order.Id },
				order);
		}
		catch (InvalidOperationException exception)
		{
			return Conflict(new
			{
				message = exception.Message
			});
		}
		catch (HttpRequestException exception)
		{
			return StatusCode(
				StatusCodes.Status502BadGateway,
				new
				{
					message =
						"A dependent service failed while processing the order.",
					detail = exception.Message
				});
		}
	}

	[HttpGet]
	public async Task<ActionResult<IReadOnlyCollection<OrderResponse>>> GetAll(
		CancellationToken cancellationToken)
	{
		var userId = GetCurrentUserId();
		var isAdmin = User.IsInRole("Admin");

		var orders = await _orderService.GetOrdersAsync(
			userId,
			isAdmin,
			cancellationToken);

		return Ok(orders);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<OrderResponse>> GetById(
		Guid id,
		CancellationToken cancellationToken)
	{
		var userId = GetCurrentUserId();
		var isAdmin = User.IsInRole("Admin");

		var order = await _orderService.GetByIdAsync(
			id,
			userId,
			isAdmin,
			cancellationToken);

		if (order is null)
		{
			return NotFound(new
			{
				message = "Order not found."
			});
		}

		return Ok(order);
	}

	private Guid GetCurrentUserId()
	{
		var userIdClaim =
			User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (!Guid.TryParse(userIdClaim, out var userId))
		{
			throw new UnauthorizedAccessException(
				"User identifier is missing or invalid.");
		}

		return userId;
	}
}