using XuanEmart.Models;
using XuanEmart.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace XuanEmart.Controllers
{
	public class CategoryController : Controller
	{
		private readonly DataContext _dataContext;

		public CategoryController(DataContext context)
		{
			_dataContext = context;
		}

		// Action này sẽ trả về danh sách các danh mục
		public async Task<IActionResult> GetCategories()
		{
			var categories = await _dataContext.Categories.ToListAsync();
			return PartialView("_CategoriesList", categories); // Trả về PartialView chứa danh sách các danh mục
		}

		public async Task<IActionResult> Index(string Slug = "")
		{
			CategoryModel category = _dataContext.Categories
				.Where(c => c.Slug == Slug)
				.FirstOrDefault();

			if (category == null) return RedirectToAction("Index");

			var ProductByCategory = _dataContext.Products
				.Where(p => p.CategoryId == category.Id);

			return View(await ProductByCategory.OrderByDescending(p => p.Id).ToListAsync());
		}
	}
}
