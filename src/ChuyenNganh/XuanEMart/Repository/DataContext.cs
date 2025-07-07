using XuanEmart.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace XuanEmart.Repository
{
    public class DataContext : IdentityDbContext<AppUserModel>
	{
		public DataContext(DbContextOptions<DataContext> options) : base(options) 
		{
			
		}

		public DbSet<BrandModel> Brands { get; set; }
		public DbSet<ProductModel> Products { get; set; }
		public DbSet<CategoryModel> Categories { get; set; }
		public DbSet<OrderModel> Orders { get; set; }
		public DbSet<OrderDetails> OrderDetails { get; set; }
		public DbSet<RatingModel> Ratings { get; set; }
		public DbSet<BillingAddressModel> BillingAddresses { get; set; }
		public DbSet<Conversations> Conversations { get; set; }
		public DbSet<ChatMessage> ChatMessages { get; set; }
		public DbSet<UserModel> Users { get; set; }
		public DbSet<IdentityRole> Roles { get; set; }
		public DbSet<IdentityUserRole<string>> UserRoles { get; set; }

	}
}
