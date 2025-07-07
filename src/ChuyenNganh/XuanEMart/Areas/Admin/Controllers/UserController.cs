using XuanEmart.Models;
using XuanEmart.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace XuanEmart.Areas.Admin.Controllers
{
    [Area("Admin")]
   [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _dataContext;

        public UserController(UserManager<AppUserModel> userManager, RoleManager<IdentityRole> roleManager, DataContext dataContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dataContext = dataContext;

        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Liên kết data 3 bảng chuẩn Identity
            var usersWithRoles = await (
                from u in _dataContext.Set<AppUserModel>()
                                        join ur in _dataContext.UserRoles on u.Id equals ur.UserId into userRoles
                                        from ur in userRoles.DefaultIfEmpty()
                                        join r in _dataContext.Roles on ur.RoleId equals r.Id into roles
                                        from r in roles.DefaultIfEmpty()
                                        select new { User = u, RoleName = r != null ? r.Name : "Chưa phân quyền" })
                             .ToListAsync();
            return View(usersWithRoles);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(new AppUserModel());
        }

       [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, AppUserModel user)
        {
            var existingUser = await _userManager.FindByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var roles = await _roleManager.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(roles, "Id", "Name");
                return View(user);
            }

            // Cập nhật thông tin người dùng
            existingUser.UserName = user.UserName;
            existingUser.Email = user.Email;
            existingUser.PhoneNumber = user.PhoneNumber;
            existingUser.RoleId = user.RoleId;

            var updateUserResult = await _userManager.UpdateAsync(existingUser);
            if (!updateUserResult.Succeeded)
            {
                AddIdentityErrors(updateUserResult);
                var roles = await _roleManager.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(roles, "Id", "Name");
                return View(user);
            }

            // Cập nhật vai trò của người dùng trong bảng liên kết
            var currentRoles = await _userManager.GetRolesAsync(existingUser);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(existingUser, currentRoles); // Xóa các vai trò hiện tại
            }

            var newRole = await _roleManager.FindByIdAsync(user.RoleId);
            if (newRole != null)
            {
                await _userManager.AddToRoleAsync(existingUser, newRole.Name); // Gán vai trò mới
            }
            else
            {
                ModelState.AddModelError("", "Vai trò không hợp lệ.");
                var roles = await _roleManager.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(roles, "Id", "Name");
                return View(user);
            }

            TempData["success"] = "Cập nhật người dùng thành công.";
            return RedirectToAction("Index", "User");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public async Task<IActionResult> Create(AppUserModel user)
        {
            if (ModelState.IsValid)
            {
                // Check if the username already exists
                var existingUser = await _userManager.FindByNameAsync(user.UserName);
                if (existingUser != null)
                {
                    ModelState.AddModelError("UserName", "Tên người dùng đã tồn tại.");
                    TempData["error"] = "Tên người dùng đã tồn tại!";
                    return View(user);
                }

                var createUserResult = await _userManager.CreateAsync(user, user.PasswordHash);
                if (createUserResult.Succeeded)
                {
                    var createUser = await _userManager.Users.Where(u => u.Email == user.Email).FirstOrDefaultAsync();
                    var userId = createUser.Id;
                    var role = await _roleManager.FindByIdAsync(user.RoleId);

                    if (role != null)
                    {
                        var addToRoleResult = await _userManager.AddToRoleAsync(createUser, role.Name);
                        if (!addToRoleResult.Succeeded)
                        {
                            AddIdentityErrors(createUserResult);
                            return View(user);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Role", "Vai trò không hợp lệ.");
                        return View(user);
                    }
                    return RedirectToAction("Index", "User");
                }
                else
                {
                    AddIdentityErrors(createUserResult);
                    return View(user);
                }
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

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(user);
        }


        [HttpGet]
        [Route("Delete")]
        public async Task<IActionResult> Delete(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return View("Error");
            }
            TempData["Success"] = "Xóa Người dùng thành công!";
            return RedirectToAction("Index");
        }

        private void AddIdentityErrors(IdentityResult identityResult)
        {
            foreach(var error in identityResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
