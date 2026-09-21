using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Common.Interfaces;

// Renders an order list snapshot into an .xlsx workbook. Implemented in Infrastructure
// (ClosedXML) to keep the concrete spreadsheet dependency out of the Application layer.
public interface IOrderListExcelExporter
{
    byte[] Export(OrderListExportModel model);
}
