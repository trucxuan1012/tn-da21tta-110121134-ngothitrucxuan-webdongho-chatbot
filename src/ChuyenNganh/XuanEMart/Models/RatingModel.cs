using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XuanEmart.Models
{
	public class RatingModel
	{
		[Key]
		public int Id { get; set; }
		[Required(ErrorMessage = "ProductId is required.")]
		public long ProductId { get; set; }
		public string Comment { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public string Star {  get; set; }

		[ForeignKey("ProductId")]
		public ProductModel Product { get; set; }
	}
}
