using OmniCore.Application.DTOs.Order;
using OmniCore.Application.DTOs.Orders;
using OmniCore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Application.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request);
        Task<OrderResponse?> GetByIdAsync(Guid orderId);
        Task<List<OrderResponse>> GetAllByCustomerAsync(Guid customerId);
        Task CancelOrderAsync(Guid orderId);
    }
}
