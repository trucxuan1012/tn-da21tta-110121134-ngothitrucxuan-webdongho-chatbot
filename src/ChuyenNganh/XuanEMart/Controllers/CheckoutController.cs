using XuanEmart.Areas.Admin.Repository;
using XuanEmart.Models;
using XuanEmart.Models.ViewModels;
using XuanEmart.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace XuanEmart.Controllers
{
	public class CheckoutController : Controller
	{
		private readonly DataContext _dataContext;
        private readonly IEmailSender _emailSender;
        public CheckoutController(DataContext context, IEmailSender emailSender)
		{
            _emailSender = emailSender;
            _dataContext = context;

		}

		public IActionResult Checkout()
		{
			var userEmail = User.FindFirstValue(ClaimTypes.Email);
			if (userEmail == null)
			{
				return RedirectToAction("Login", "Account");
			}

			var orderCode = Guid.NewGuid().ToString();
			HttpContext.Session.SetString("OrderCode", orderCode);

			// Trả về view 'CheckoutBillingAddress'
			return View("CheckoutBillingAddress");
		}

		[HttpPost]
		public async Task<IActionResult> SaveBillingAddress(BillingAddressModel billingAddress)
		{
			if (!ModelState.IsValid)
			{
				return View("CheckoutBillingAddress", billingAddress);
			}

			var userEmail = User.FindFirstValue(ClaimTypes.Email);
			if (userEmail == null)
			{
				return RedirectToAction("Login", "Account");
			}

			var orderCode = HttpContext.Session.GetString("OrderCode");
			if (orderCode == null)
			{
				return RedirectToAction("Cart", "Cart");
			}

			// Tạo đơn hàng
			var orderItem = new OrderModel
			{
				OrderCode = orderCode,
				UserName = userEmail,
				Status = 1,
				CreatedDate = DateTime.Now
			};
			_dataContext.Add(orderItem);
			_dataContext.SaveChanges();

			// Lưu thông tin thanh toán
			billingAddress.OrderId = orderItem.Id;
			_dataContext.Add(billingAddress);
			_dataContext.SaveChanges();

			// Lưu chi tiết đơn hàng
			List<CartItemModel> cartItems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
			foreach (var cart in cartItems)
			{
				var orderDetails = new OrderDetails
				{
					UserName = userEmail,
					OrderCode = orderCode,
					ProductId = cart.ProductId,
					Price = cart.Price,
					Quantity = cart.Quantity
				};
				_dataContext.Add(orderDetails);
			}
			_dataContext.SaveChanges();

			HttpContext.Session.Remove("Cart");

			//Gửi email thông báo đơn hàng
            #region Sendmail
            var receiver = userEmail;
			var subject = "XuannEShop - Đặt hàng thành công";
			var message = $@"
		                  <!DOCTYPE html>
		                  <html>
		                  <head>
		                      <style>
		                          body {{
		                              font-family: Arial, sans-serif;
		                              line-height: 1.6;
		                              margin: 0;
		                              padding: 0;
		                              background-color: #f9f9f9;
		                          }}
		                          .email-container {{
		                              max-width: 600px;
		                              margin: 0 auto;
		                              background-color: #ffffff;
		                              border: 1px solid #eaeaea;
		                              padding: 20px;
		                              text-align: center;
		                          }}
		                          .email-header {{
		                              margin-bottom: 20px;
		                          }}
		                          .email-header img {{
		                              width: 150px;
		                          }}
		                          .email-body {{
		                              text-align: left;
		                              font-size: 14px;
		                              color: #333333;
		                          }}
		                          .email-body a{{
		                              color: white;
		                          }}
		                          .cta-button {{
		                              display: inline-block;
		                              margin-top: 15px;
		                              padding: 12px 25px;
		                              background-color: #ff5722;
		                              color: white;
		                              text-decoration: none;
		                              font-size: 16px;
		                              font-weight: bold; 
		                              border-radius: 8px; 
		                              box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); 
		                              transition: background-color 0.3s ease, transform 0.3s ease;
		                          }}
		                          .email-footer {{
		                              margin-top: 20px;
		                              font-size: 12px;
		                              color: #888888;
		                          }}
		                          .email-footer a {{
		                              color: #007bff;
		                              text-decoration: none;
		                          }}
		                      </style>
		                  </head>
		                  <body>
		                      <div class='email-container'>
		                          <div class='email-header'>
		                              <img src='https://via.placeholder.com/150' alt='XuannEShop Logo'>
		                          </div>
		                          <div class='email-body'>
		                              <p>Xin chào <b>{userEmail}</b>,</p>
		                              <p>Chúc mừng bạn đã đặt hàng thành công tại <b>XuannEShop</b>!</p>
		                              <p>Đơn hàng của bạn với mã số <b>{orderCode}</b> đã được ghi nhận và đang được chúng tôi xử lý.</p>
		                              <p>Chúng tôi sẽ cập nhật trạng thái đơn hàng trong thời gian sớm nhất. Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua email hoặc hotline.</p>
		                              <a href='https://hieueshop.vn/order-details/{orderCode}' class='cta-button'>Xem chi tiết đơn hàng</a>
		                          </div>
		                          <div class='email-footer'>
		                              <p>Cảm ơn bạn đã tin tưởng và lựa chọn <b>XuannEShop</b>.</p>
		                              <p>Trân trọng,<br>Đội ngũ XuannEShop</p>
		                              <p><a href='#'>Chính sách bảo mật</a> | <a href='#'>Điều khoản dịch vụ</a></p>
		                              <p>Đây là email tự động. Vui lòng không trả lời email này.</p>
		                          </div>
		                      </div>
		                  </body>
		                  </html>";
            #endregion Sendmail

            await _emailSender.SendEmailAsync(receiver, subject, message);

			TempData["success"] = "Đặt hàng thành công. Vui lòng chờ duyệt đơn hàng.";
			return RedirectToAction("Index", "Cart");
		}

	}
}