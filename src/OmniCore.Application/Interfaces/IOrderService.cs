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
        Task<Guid> PlaceOrderAsync(Guid customerId, CreateOrderRequest request);
    }
}
