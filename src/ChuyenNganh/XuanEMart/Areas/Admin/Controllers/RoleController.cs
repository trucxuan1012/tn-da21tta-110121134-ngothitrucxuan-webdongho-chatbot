using XuanEmart.Models;
using XuanEmart.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace XuanEmart.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly RoleManager<IdentityRole> _roleManager;
        public RoleController(DataContext context, RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
            _dataContext = context;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _dataContext.Roles.OrderByDescending(p => p.Id).ToListAsync());
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IdentityRole model)
        {
            if (await _roleManager.RoleExistsAsync(model.Name))
            {
                TempData["Error"] = "Vai trò đã tồn tại.";
                return RedirectToAction("Index");
            }
            var result = await _roleManager.CreateAsync(new IdentityRole(model.Name));
            if (result.Succeeded)
            {
                TempData["Success"] = "Vai trò đã được tạo thành công.";
            }
            else
            {
                TempData["Error"] = "Đã xảy ra lỗi trong quá trình tạo vai trò.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "ID vai trò không hợp lệ.";
                return RedirectToAction("Index");
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                TempData["Error"] = "Vai trò không tồn tại.";
                return RedirectToAction("Index");
            }

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(IdentityRole model)
        {
            if (ModelState.IsValid)
            {
                var role = await _roleManager.FindByIdAsync(model.Id);
                if (role == null)
                {
                    TempData["Error"] = "Vai trò không tồn tại.";
                    return RedirectToAction("Index");
                }

                role.Name = model.Name; // Cập nhật tên vai trò
                var result = await _roleManager.UpdateAsync(role);

                if (result.Succeeded)
                {
                    TempData["Success"] = "Vai trò đã được cập nhật thành công.";
                }
                else
                {
                    TempData["Error"] = "Đã xảy ra lỗi trong quá trình cập nhật vai trò.";
                }

                return RedirectToAction("Index");
            }

            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "ID vai trò không hợp lệ.";
                return RedirectToAction("Index");
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                TempData["Error"] = "Vai trò không tồn tại.";
                return RedirectToAction("Index");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                TempData["success"] = "Vai trò đã được xóa thành công.";
            }
            else
            {
                TempData["error"] = "Đã xảy ra lỗi trong quá trình xóa vai trò.";
            }

            return RedirectToAction("Index");
        }


    }
}
