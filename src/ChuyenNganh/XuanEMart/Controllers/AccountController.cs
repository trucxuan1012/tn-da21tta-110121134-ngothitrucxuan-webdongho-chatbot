using XuanEmart.Models;
using XuanEmart.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace XuanEmart.Controllers
{
	public class AccountController : Controller
	{
		private UserManager<AppUserModel> _userManager;
		private SignInManager<AppUserModel> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public AccountController(SignInManager<AppUserModel> signInManager, UserManager<AppUserModel> userManager, RoleManager<IdentityRole> roleManager) 
		{
			_userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
		}
        public IActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }


        [HttpPost]
		public async Task<IActionResult> Login(LoginViewModel loginVM)
		{
			if(ModelState.IsValid)
			{
				Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(loginVM.UserName, loginVM.Password, false,false);
				if (result.Succeeded)
				{
					return Redirect(loginVM.ReturnUrl ?? "/");
				}
				TempData["error"] = "Không xác thực được Tên đăng nhập hoặc Mật khẩu";
			}
			return View(loginVM);
		}


		public IActionResult Create()
		{
			return View();
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(UserModel user)
		{
			 if (ModelState.IsValid) 
			{
				AppUserModel newUser = new AppUserModel { UserName =user.UserName, Email = user.Email};
				IdentityResult result = await _userManager.CreateAsync(newUser, user.Password);
				if (result.Succeeded)
				{
					//string roleId = "C3BB5327-2A05-4FB6-BABC-E406556939AA";
     //               var role = await _roleManager.FindByIdAsync(roleId);
                    TempData["success"] = "Tạo tài khoản thành công";
                    //await _userManager.AddToRoleAsync(newUser, role.Name);
                    return Redirect("/account/login");
				}

				foreach(IdentityError error in result.Errors) 
				{
					ModelState.AddModelError("", error.Description);
				}
			}
			return View(user);
		}
		public async Task<IActionResult> Logout( string returnUrl = "/")
		{
			await _signInManager.SignOutAsync();
			return Redirect(returnUrl);
		}
	}
}
