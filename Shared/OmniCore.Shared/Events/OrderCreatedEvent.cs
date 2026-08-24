namespace OmniCore.Shared.Events;

public class OrderCreatedEvent
{
	public Guid OrderId { get; set; }

	public string OrderNumber { get; set; } = string.Empty;

	public Guid UserId { get; set; }

	public decimal TotalAmount { get; set; }

	public DateTime CreatedAt { get; set; }
}