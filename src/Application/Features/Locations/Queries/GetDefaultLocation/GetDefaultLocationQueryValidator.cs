namespace skestock.Application.Features.Locations.Queries.GetDefaultLocation;

// The query carries no user input; the validator exists to keep the slice consistent with the
// rest of the Locations feature and to provide a hook for future rules.
public class GetDefaultLocationQueryValidator : AbstractValidator<GetDefaultLocationQuery>
{
    public GetDefaultLocationQueryValidator()
    {
    }
}
