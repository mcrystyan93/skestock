using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.Features.SchoolClasses.Commands.InitializeSchoolClassConfiguration;

public sealed class InitializeSchoolClassConfigurationCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<InitializeSchoolClassConfigurationCommand, Result>
{
    public async ValueTask<Result> Handle(InitializeSchoolClassConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var configuration = await dbContext.SharedClassConfigurations
                .AsNoTracking()
                .Include(item => item.DepartmentTemplates)
                .SingleOrDefaultAsync(item => item.Id == SharedClassConfiguration.SingletonId, cancellationToken);
            if (configuration is null)
                return Result.Fail(new SchoolClassErrors.ConfigurationRequired());

            var classExists = await dbContext.SchoolClasses
                .AnyAsync(item => item.Id == request.SchoolClassId, cancellationToken);
            if (!classExists)
                return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.SchoolClassId));

            // Conditional update is the claim: concurrent requests can only initialize the class once.
            var claimed = await dbContext.SchoolClasses
                .Where(item => item.Id == request.SchoolClassId && item.InvitationCount == null)
                .ExecuteUpdateAsync(
                    update => update.SetProperty(item => item.InvitationCount, configuration.InvitationCount),
                    cancellationToken);
            if (claimed == 0)
                return Result.Fail(new SchoolClassErrors.ConfigurationAlreadyInitialized(request.SchoolClassId));

            var schoolClass = await dbContext.SchoolClasses
                .Include(item => item.Departments)
                .SingleAsync(item => item.Id == request.SchoolClassId, cancellationToken);
            foreach (var template in configuration.DepartmentTemplates)
            {
                schoolClass.Departments.Add(new ClassDepartment
                {
                    Name = template.Name, Responsibilities = template.Responsibilities
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Ok();
        });
    }
}
