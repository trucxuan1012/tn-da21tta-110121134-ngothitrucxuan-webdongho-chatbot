using XuanEmart.Models;
using XuanEmart.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace XuanEmart.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, Staff")]
    public class OrderController : Controller
	{
        private readonly DataContext _dataContext;
        public OrderController(DataContext context) {
            _dataContext = context;
        }
        public async Task<IActionResult> Index(int pg = 1)
        {
            List<OrderModel> order = _dataContext.Orders.ToList();

            const int pageSize = 10;

            if (pg < 1)
            {
                pg = 1;
            }
            int rescCount = order.Count();
            var pager = new Paginate(rescCount, pg, pageSize);
            int recSkip = (pg - 1) * pageSize;
            var data = order.Skip(recSkip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;
            return View(data);
        }
       
        [HttpPost]
        public async Task<IActionResult> UpdateOrder(string ordercode, int status)
        {
            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == ordercode);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            _dataContext.Orders.Update(order);
            await _dataContext.SaveChangesAsync();

            return Json(new { success = true, redirectUrl = Url.Action("Index") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(string ordercode)
        {
            // Tìm đơn hàng theo mã
            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == ordercode);
            if (order == null)
            {
                TempData["error"] = "Đơn hàng không tồn tại.";
                return RedirectToAction("Index");
            }

            // Xóa chi tiết đơn hàng liên quan
            var orderDetails = await _dataContext.OrderDetails.Where(od => od.OrderCode == ordercode).ToListAsync();
            if (orderDetails.Any())
            {
                _dataContext.OrderDetails.RemoveRange(orderDetails);
            }

            // Xóa địa chỉ thanh toán liên quan (nếu có)
            var billingAddress = await _dataContext.BillingAddresses.FirstOrDefaultAsync(b => b.OrderId == order.Id);
            if (billingAddress != null)
            {
                _dataContext.BillingAddresses.Remove(billingAddress);
            }

            // Xóa đơn hàng
            _dataContext.Orders.Remove(order);

            // Lưu thay đổi vào cơ sở dữ liệu
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Đã xóa đơn hàng thành công.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ViewOrder(string ordercode)
        {
            var detailsOrder = await _dataContext.OrderDetails
                .Include(o => o.Product)
                .Where(o => o.OrderCode == ordercode)
                .ToListAsync();

            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == ordercode);

            var billingAddress = await _dataContext.BillingAddresses
                .FirstOrDefaultAsync(b => b.OrderId == order.Id);

            ViewData["OrderCode"] = ordercode;
            ViewData["BillingAddress"] = billingAddress;

            return View(detailsOrder);
        }


        [HttpGet]
        public async Task<IActionResult> ExportOrderToPdf(string ordercode)
        {
            try
            {
                var detailsOrder = await _dataContext.OrderDetails
                    .Include(o => o.Product)
                    .Where(o => o.OrderCode == ordercode)
                    .ToListAsync();

                var order = await _dataContext.Orders
                    .FirstOrDefaultAsync(o => o.OrderCode == ordercode);

                var billingAddress = await _dataContext.BillingAddresses
                    .FirstOrDefaultAsync(b => b.OrderId == order.Id);

                // Tạo tài liệu PDF
                var document = new PdfDocument();
                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);

                // Thiết lập font chữ
                var titleFont = new XFont("Arial", 20, XFontStyle.Bold);
                var contentFont = new XFont("Arial", 12, XFontStyle.Regular);
                var headerFont = new XFont("Arial", 12, XFontStyle.Bold);

                // Vẽ tiêu đề
                gfx.DrawString("Thông Tin Đơn Hàng", titleFont, XBrushes.Black, new XRect(0, 40, page.Width, 40), XStringFormats.TopCenter);
                gfx.DrawString($"Mã Đơn Hàng: {ordercode}", contentFont, XBrushes.Black, new XRect(0, 80, page.Width, 20), XStringFormats.TopCenter);

                // Vẽ thông tin khách hàng
                gfx.DrawString("Thông Tin Khách Hàng:", headerFont, XBrushes.Black, new XRect(40, 120, page.Width, 20), XStringFormats.TopLeft);
                gfx.DrawString($"- Họ và Tên: {billingAddress.FullName}", contentFont, XBrushes.Black, new XRect(60, 140, page.Width, 20), XStringFormats.TopLeft);
                gfx.DrawString($"- Số Điện Thoại: {billingAddress.PhoneNumber}", contentFont, XBrushes.Black, new XRect(60, 160, page.Width, 20), XStringFormats.TopLeft);
                gfx.DrawString($"- Địa Chỉ: {billingAddress.SpecificAddress}, {billingAddress.Ward}, {billingAddress.District}, {billingAddress.Province}", contentFont, XBrushes.Black, new XRect(60, 180, page.Width, 40), XStringFormats.TopLeft);

                // Vẽ bảng chi tiết sản phẩm
                double yOffset = 240;

                // Header của bảng
                gfx.DrawRectangle(XBrushes.LightGray, 40, yOffset - 20, page.Width - 80, 20);
                gfx.DrawString("Tên Sản Phẩm", headerFont, XBrushes.Black, new XRect(50, yOffset - 18, 150, 20), XStringFormats.TopLeft);
                gfx.DrawString("Số Lượng", headerFont, XBrushes.Black, new XRect(250, yOffset - 18, 100, 20), XStringFormats.TopLeft);
                gfx.DrawString("Giá Sản Phẩm", headerFont, XBrushes.Black, new XRect(350, yOffset - 18, 100, 20), XStringFormats.TopLeft);
                gfx.DrawString("Tổng", headerFont, XBrushes.Black, new XRect(450, yOffset - 18, 100, 20), XStringFormats.TopLeft);

                yOffset += 20;

                decimal total = 0;

                // Duyệt qua từng sản phẩm
                foreach (var item in detailsOrder)
                {
                    decimal subtotal = item.Quantity * item.Price;
                    total += subtotal;

                    // Chia tên sản phẩm thành nhiều dòng
                    var productNameLines = SplitText(item.Product.Name, contentFont, 150, gfx);
                    double lineOffset = 0;

                    // Vẽ từng dòng của tên sản phẩm
                    foreach (var line in productNameLines)
                    {
                        gfx.DrawString(line, contentFont, XBrushes.Black, new XRect(50, yOffset + lineOffset, 150, 20), XStringFormats.TopLeft);
                        lineOffset += 20;
                    }

                    // Vẽ các cột khác
                    gfx.DrawString(item.Quantity.ToString(), contentFont, XBrushes.Black, new XRect(250, yOffset, 100, 20), XStringFormats.TopLeft);
                    gfx.DrawString(item.Price.ToString("N0"), contentFont, XBrushes.Black, new XRect(350, yOffset, 100, 20), XStringFormats.TopLeft);
                    gfx.DrawString(subtotal.ToString("N0"), contentFont, XBrushes.Black, new XRect(450, yOffset, 100, 20), XStringFormats.TopLeft);

                    // Tăng yOffset dựa trên chiều cao thực tế của các dòng
                    yOffset += Math.Max(lineOffset, 20);
                }

                // Tổng cộng
                yOffset += 20;
                gfx.DrawString("Tổng Cộng:", headerFont, XBrushes.Black, new XRect(350, yOffset + 10, 100, 20), XStringFormats.TopLeft);
                gfx.DrawString(total.ToString("N0"), headerFont, XBrushes.Black, new XRect(450, yOffset + 10, 100, 20), XStringFormats.TopLeft);

                // Xuất PDF ra file
                using (var stream = new MemoryStream())
                {
                    document.Save(stream, false);
                    return File(stream.ToArray(), "application/pdf", $"Order_{ordercode}.pdf");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        // Hàm chia chuỗi dài thành nhiều dòng
        private static List<string> SplitText(string text, XFont font, double maxWidth, XGraphics gfx)
        {
            var lines = new List<string>();
            var words = text.Split(' ');
            string currentLine = "";

            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                var size = gfx.MeasureString(testLine, font);

                if (size.Width > maxWidth)
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            return lines;
        }

        [HttpGet]
        public async Task<IActionResult> OrderByDay()
        {
            var ordersByDay = await _dataContext.Orders
                .Where(o => o.Status == 3) // ví dụ: chỉ tính đơn đã giao
                .GroupBy(o => o.CreatedDate.Date)
                .Select(g => new
                {
                    Date = g.Key,         // dạng DateTime
                    Total = g.Count()     // số đơn trong ngày
                })
                .OrderBy(g => g.Date)
                .ToListAsync();

            return View(ordersByDay);
        }




    }
}
