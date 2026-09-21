namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;

public static class OrderListErrors
{
    public sealed class OrderListNotFound : Error
    {
        public const string ErrorCode = "order_lists.not_found";

        public OrderListNotFound(Guid orderListId) : base($"Order list with id '{orderListId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Order list not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["orderListId"] = orderListId });
        }
    }

    // Editing lines/metadata or deleting a submitted list, submitting a non-draft list, etc.
    public sealed class OrderListNotEditable : Error
    {
        public const string ErrorCode = "order_lists.not_editable";

        public OrderListNotEditable(Guid orderListId, string status) : base(
            $"Order list '{orderListId}' cannot be modified while in status '{status}'.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Order list not editable");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["orderListId"] = orderListId,
                ["status"] = status
            });
        }
    }

    public sealed class OrderListNotDeletable : Error
    {
        public const string ErrorCode = "order_lists.not_deletable";

        public OrderListNotDeletable(Guid orderListId, string status) : base(
            $"Order list '{orderListId}' cannot be deleted while in status '{status}'. Cancel it first.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Order list not deletable");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["orderListId"] = orderListId,
                ["status"] = status
            });
        }
    }

    public sealed class OrderListEmpty : Error
    {
        public const string ErrorCode = "order_lists.empty";

        public OrderListEmpty(Guid orderListId) : base(
            $"Order list '{orderListId}' cannot be submitted without at least one line.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status422UnprocessableEntity);
            Metadata.Add(ErrorMetadataKeys.Title, "Order list is empty");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["orderListId"] = orderListId });
        }
    }

    public sealed class OrderListAlreadyCancelled : Error
    {
        public const string ErrorCode = "order_lists.already_cancelled";

        public OrderListAlreadyCancelled(Guid orderListId) : base(
            $"Order list '{orderListId}' is already cancelled.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Order list already cancelled");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["orderListId"] = orderListId });
        }
    }

    // Only submitted order lists can be exported to Excel.
    public sealed class OrderListNotExportable : Error
    {
        public const string ErrorCode = "order_lists.not_exportable";

        public OrderListNotExportable(Guid orderListId, string status) : base(
            $"Order list '{orderListId}' cannot be exported while in status '{status}'. Only submitted order lists can be exported.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Order list not exportable");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["orderListId"] = orderListId,
                ["status"] = status
            });
        }
    }

    public sealed class OrderListNotReopenable : Error
    {
        public const string ErrorCode = "order_lists.not_reopenable";

        public OrderListNotReopenable(Guid orderListId, string status) : base(
            $"Order list '{orderListId}' cannot be reopened while in status '{status}'. Only cancelled order lists can be reopened.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Order list not reopenable");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["orderListId"] = orderListId,
                ["status"] = status
            });
        }
    }
}
