using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.SupplyLists.Commands.CreateSupplyList;

public class CreateSupplyListCommandValidator(IApplicationDbContext dbContext)
    : SupplyListPayloadValidator<CreateSupplyListCommand>(dbContext, _ => null);
