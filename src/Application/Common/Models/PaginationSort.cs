namespace skestock.Application.Common.Models;

public class PaginationSort
{
    public required string Key { get; set; }
    public required string Value { get; set; } // 'ascend' | 'descend'
}
