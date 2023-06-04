using MediatR;

namespace Application.Features.OrderEvents.Queries;

public class OrderEventGetAllQueryHandler:IRequestHandler<OrderEventGetAllQuery,List<OrderEventGetAllDto>>
{
    public Task<List<OrderEventGetAllDto>> Handle(OrderEventGetAllQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}