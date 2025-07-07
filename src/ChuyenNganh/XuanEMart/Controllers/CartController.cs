using XuanEmart.Models;
using XuanEmart.Models.ViewModels;
using XuanEmart.Repository;
using Microsoft.AspNetCore.Mvc;

namespace XuanEmart.Controllers
{
	public class CartController : Controller
	{
		private readonly DataContext _dataContext;
		public CartController(DataContext _context)
		{
			_dataContext = _context;

		}
		public IActionResult Index()
		{
			List<CartItemModel> cartItems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
			CartItemViewModel cartVM = new()
			{
				CartItems = cartItems,
				GrandTotal = cartItems.Sum(x => x.Quantity * x.Price)
			};
			return View(cartVM);
		}
		public ActionResult Checkout()
		{
			return View("~/Views/Checkout/Index.cshtml");
		}
		public async Task<IActionResult> Add(int Id)
		{
			// Kiểm tra xem người dùng đã đăng nhập chưa
			if (!User.Identity.IsAuthenticated)
			{
				// Trả về thông báo yêu cầu đăng nhập
				return Json(new { loggedIn = false });
			}

			// Lấy sản phẩm từ cơ sở dữ liệu
			ProductModel product = await _dataContext.Products.FindAsync((long)Id);

			// Lấy giỏ hàng từ session, nếu không có thì tạo mới
			List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();

			// Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
			CartItemModel cartItems = cart.FirstOrDefault(c => c.ProductId == Id);

			if (cartItems == null)
			{
				// Nếu chưa có, thêm sản phẩm vào giỏ hàng
				cart.Add(new CartItemModel(product));
			}
			else
			{
				// Nếu đã có, tăng số lượng sản phẩm
				cartItems.Quantity += 1;
			}

			// Lưu giỏ hàng vào session
			HttpContext.Session.SetJson("Cart", cart);

			// Trả về thông báo thành công và giỏ hàng
			return Json(new { loggedIn = true, message = "Thêm sản phẩm vào giỏ hàng thành công!" });
		}

		public async Task<IActionResult> Decrease(int Id)
		{
			List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");

			CartItemModel carItem = cart.Where(c => c.ProductId == Id).FirstOrDefault();

			if (carItem != null)
			{
				if (carItem.Quantity > 1)
				{
					--carItem.Quantity;
				}
				else
				{
					cart.Remove(carItem);
				}
			}
			HttpContext.Session.SetJson("Cart", cart);
			return RedirectToAction("Index");
		}

		public async Task<IActionResult> Increase(int Id)
		{
			List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");

			CartItemModel carItem = cart.Where(c => c.ProductId == Id).FirstOrDefault();

			if (carItem.Quantity != null)
			{
				++carItem.Quantity;
			}
			HttpContext.Session.SetJson("Cart", cart);

			return RedirectToAction("Index");
		}
		public async Task<IActionResult> Remove(int Id)
		{
			List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
			{
				cart.RemoveAll(p => p.ProductId == Id);
				if (cart.Count == 0)
				{
					HttpContext.Session.Remove("Cart");
				}
				else
				{
					HttpContext.Session.SetJson("Cart", cart);
				}
			}
			return RedirectToAction("Index");
		}
		public async Task<IActionResult> Clear(int Id)
		{
			List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
			{
				HttpContext.Session.Remove("Cart");
				TempData["success"] = "Xóa thành công toàn bộ sản phẩm!";
				return RedirectToAction("Index");
                
            }
		}
	}
}