using XuanEmart.Models;
using XuanEmart.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace XuanEmart.Areas.Admin.Controllers
{
    [Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class BrandController : Controller
    {
        private readonly DataContext _dataContext;
        public BrandController(DataContext context)
        {
            _dataContext = context;
        }


        public async Task<IActionResult> Index(int pg = 1)
        {
			List<BrandModel> brand = _dataContext.Brands.ToList();

			const int pageSize = 10;

			if (pg < 1)
			{
				pg = 1;
			}
			int rescCount = brand.Count();
			var pager = new Paginate(rescCount, pg, pageSize);
			int recSkip = (pg - 1) * pageSize;
			var data = brand.Skip(recSkip).Take(pager.PageSize).ToList();
			ViewBag.Pager = pager;
			return View(data);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandModel brand)
        {
            if (ModelState.IsValid)
            {
                brand.Slug = brand.Name.Replace(" ", "-");
                var slug = await _dataContext.Brands.FirstOrDefaultAsync(p => p.Slug == brand.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "Thương hiệu đã có trong cơ sở dữ liệu");
                    return View(brand);
                }
                _dataContext.Add(brand);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Đã thêm thành công";
                return RedirectToAction("Index");
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
                return BadRequest(errorMessage);
            }
            return View(brand);
        }
		[HttpGet]
		[Route("Admin/Brand/Edit/{id}")]
		public async Task<IActionResult> Edit(int id)
		{
			BrandModel brand = await _dataContext.Brands.FindAsync(id);
			if (brand == null)
			{
				return NotFound();
			}
			return View(brand);
		}
		[HttpPost]
		[Route("Admin/Brand/Edit/{id}")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(BrandModel brand)
		{
			if (ModelState.IsValid)
			{
				brand.Slug = brand.Name.Replace(" ", "-");
				var slug = await _dataContext.Brands.FirstOrDefaultAsync(p => p.Slug == brand.Slug && p.Id != brand.Id);
				if (slug != null)
				{
					ModelState.AddModelError("", "Danh mục đã có trong cơ sở dữ liệu");
					return View(brand);
				}
				_dataContext.Update(brand);
				await _dataContext.SaveChangesAsync();
				TempData["success"] = "Cập nhật thành công";
				return RedirectToAction("Index");
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
				return BadRequest(errorMessage);
			}
			return View(brand);
		}
		public async Task<IActionResult> Delete(int Id)
        {
            BrandModel brand = await _dataContext.Brands.FindAsync(Id);
            if (brand == null)
            {
                return NotFound(); // Không tìm thấy
            }
            _dataContext.Brands.Remove(brand);
            await _dataContext.SaveChangesAsync();
            TempData["Success"] = "Xóa Thương hiệu thành công!";
            return RedirectToAction("Index");
        }
    }
}

