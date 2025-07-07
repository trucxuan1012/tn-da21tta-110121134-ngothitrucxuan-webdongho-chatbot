namespace XuanEmart.Models.ViewModels
{
	public class ProductDetailsViewModel
	{
		public ProductModel ProductDetails { get; set; }
		public List<RatingModel> Ratings { get; set; }
		public string Comment { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public string Star { get; set; }
	}
}
