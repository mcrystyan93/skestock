namespace skestock.Application.Features.OrderLists.Queries.ExportOrderList;

public class ExportOrderListQueryValidator : AbstractValidator<ExportOrderListQuery>
{
    public ExportOrderListQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
