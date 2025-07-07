using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XuanEmart.Models
{
	public class BillingAddressModel
	{
		public int Id { get; set; }

		[ForeignKey("OrderModel")]
		public int OrderId { get; set; }
		public OrderModel Order { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập họ tên")]
		public string FullName { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
		public string PhoneNumber { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập địa chỉ cụ thể")]
		public string SpecificAddress { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập Phường/Xã")]
		public string Ward { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn Quận/Huyện")]
		public string District { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành")]
		public string Province { get; set; }
	}
}
