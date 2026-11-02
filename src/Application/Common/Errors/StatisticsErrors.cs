using Microsoft.AspNetCore.Http;

namespace skestock.Application.Common.Errors;

public static class StatisticsErrors
{
    public sealed class RunInProgress : Error
    {
        public const string ErrorCode = "statistics.run_in_progress";

        public RunInProgress() : base("Purchase statistics are already being recomputed.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Statistics run in progress");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
        }
    }
}
