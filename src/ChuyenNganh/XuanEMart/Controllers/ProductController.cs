using XuanEmart.Models;
using XuanEmart.Models.ViewModels;
using XuanEmart.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace XuanEmart.Controllers
{
    public class ProductController : Controller
	{
		private readonly DataContext _dataContext;
		public ProductController(DataContext context)
		{
			_dataContext = context;

		}
		public IActionResult Index()
		{
			return View();
		}
		public async Task<IActionResult> Details(int? Id)
		{
			if (Id == null) return RedirectToAction("Index");

			var ProductById = await _dataContext.Products.Include(p => p.Ratings)
														 .Include(p => p.Brand)
														 .Include(p => p.Category)
														 .Where(p => p.Id == Id).FirstOrDefaultAsync();

			if (ProductById == null) return NotFound();

			// Sản phẩm liên quan
			var relatedProducts = await _dataContext.Products
				.Where(p => p.CategoryId == ProductById.CategoryId && p.Id != ProductById.Id)
				.Take(4)
				.ToListAsync();

			ViewBag.RelatedProducts = relatedProducts;

			var viewModel = new ProductDetailsViewModel
			{
				ProductDetails = ProductById,
				Ratings = ProductById.Ratings.ToList()
			};

			return View(viewModel);
		}


		public async Task<IActionResult> Search(string searchTerm)
		{
			var products = await _dataContext.Products
				.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
				.ToListAsync();
			ViewBag.Keyword = searchTerm;

			return View(products);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CommentProduct(RatingModel rating)
		{

			if (ModelState.IsValid)
			{
				var ratingEntity = new RatingModel
				{
					ProductId = rating.ProductId,
					Name = rating.Name,
					Email = rating.Email,
					Comment = rating.Comment,
					Star = rating.Star
				};

				// Thêm đánh giá vào cơ sở dữ liệu
				_dataContext.Ratings.Add(ratingEntity);
				await _dataContext.SaveChangesAsync();

				// Thông báo thành công
				TempData["success"] = "Đánh giá thành công!";
				return Redirect(Request.Headers["Referer"]);
			}
			else
			{
				TempData["error"] = "Vui lòng kiểm tra lại dữ liệu!";
				List<string> errors = new List<string>();
				foreach (var value in ModelState.Values)
				{
					foreach (var error in value.Errors)
					{
						errors.Add(error.ErrorMessage);
					}
				}
				string errorMessage = string.Join("\n", errors);
				return RedirectToAction("Detail", new { id = rating.ProductId });
			}
		}

	}
}
