using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("conversations")]
public class Conversations
{
	[Key]
	[Column("conversation_id")]
	public int Id { get; set; } // Maps to conversation_id, auto-incremented primary key

	[Column("user_id")]
	[Required]
	public string UserId { get; set; } // Maps to user_id, NOT NULL

	[Column("status")]
	[Required]
	[MaxLength(64)]
	public string Status { get; set; } // Maps to status, VARCHAR(64), NOT NULL

	[Column("is_bot_handled")]
	[Required]
	public bool IsBotHandled { get; set; } // Maps to is_bot_handled, BIT, NOT NULL, default 0

	[Column("created_at")]
	[Required]
	public DateTime CreatedAt { get; set; } // Maps to created_at, DATETIME, NOT NULL
}