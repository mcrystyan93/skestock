using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;

namespace skestock.Application.Features.ClassConfiguration.Commands.DeleteDepartment;

public sealed class DeleteDepartmentCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<DeleteDepartmentCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var configuration = await dbContext.SharedClassConfigurations.Include(item => item.DepartmentTemplates)
            .SingleOrDefaultAsync(item => item.Id == SharedClassConfiguration.SingletonId, cancellationToken);
        var department = configuration?.DepartmentTemplates.SingleOrDefault(item => item.Id == request.Id);
        if (department is null)
            return Result.Fail(new ClassConfigurationErrors.DepartmentNotFound(request.Id));
        configuration!.DepartmentTemplates.Remove(department);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
