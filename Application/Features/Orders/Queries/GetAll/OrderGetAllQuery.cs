using Application.Features.OrderEvents.Queries;
using MediatR;

namespace Application.Features.Orders.Queries.GetAll;

public class OrderGetAllQuery : IRequest<List<OrderEventGetAllDto>>, IRequest<List<OrderGetAllDto>>
{
    public OrderGetAllQuery(bool? isDeleted)
    {
        IsDeleted = isDeleted;
    }

    public bool? IsDeleted { get; set; }
}