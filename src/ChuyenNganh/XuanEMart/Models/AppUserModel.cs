using Microsoft.AspNetCore.Identity;

namespace XuanEmart.Models
{
	public class AppUserModel : IdentityUser
	{
		public string Occupation { get; set; }
		public string RoleId { get; set; }
        // Đã xóa trường Avatar
	}
}
