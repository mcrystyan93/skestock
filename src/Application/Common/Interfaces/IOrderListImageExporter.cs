using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Common.Interfaces;

// Renders an order list snapshot into a PNG image. Implemented in Infrastructure (SkiaSharp)
// to keep the concrete graphics dependency out of the Application layer.
public interface IOrderListImageExporter
{
    byte[] Export(OrderListExportModel model);
}
