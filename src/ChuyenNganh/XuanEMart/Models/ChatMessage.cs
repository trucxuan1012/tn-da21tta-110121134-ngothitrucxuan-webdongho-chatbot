using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XuanEmart.Models
{
	[Table("chat_messages")]
	public class ChatMessage
	{
		[Key]
		[Column("chat_id")]
		public int Id { get; set; } // Maps to chat_id, auto-incremented primary key

		[Column("conversation_id")]
		[Required]
		public int ConversationId { get; set; } // Maps to conversation_id, NOT NULL

		[Column("sender_type")]
		[Required]
		[MaxLength(32)]
		public string SenderType { get; set; } // Maps to sender_type, VARCHAR(32), NOT NULL

		[Column("sender_id")]
		public string? SenderId { get; set; } // Maps to sender_id, NVARCHAR, NULL

		[Column("message")]
		[MaxLength(2000)]
		public string? Message { get; set; } // Maps to message, NVARCHAR(2000), NULL

		[Column("time_stamp")]
		[Required]
		public DateTime CreatedAt { get; set; } // Maps to time_stamp, DATETIME, NOT NULL

		[Column("is_from_bot")]
		public bool? IsFromBot { get; set; } // Maps to is_from_bot, BIT, NULL
	}
}