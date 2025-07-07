using XuanEmart.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace XuanEmart.Service
{
	public interface IChatService
	{
		Task SendMessage(string conversationId, string message, string senderType, string senderId);
		Task UpdateConversationStatus(string conversationId, string status);
	}

	public class ChatService : IChatService
	{
		private readonly IHubContext<ChatHub> hubContext;

		public ChatService(IHubContext<ChatHub> hubContext)
		{
			this.hubContext = hubContext;
		}

		public async Task SendMessage(string conversationId, string message, string senderType, string senderId)
		{
			await hubContext.Clients.Group($"Conversation-{conversationId}").SendAsync("ReceiveMessage", new
			{
				ConversationId = conversationId,
				Message = message,
				SenderType = senderType,
				SenderId = senderId,
				TimeStamp = DateTime.Now
			});
		}

		public async Task UpdateConversationStatus(string conversationId, string status)
		{
			await hubContext.Clients.Group($"Conversation-{conversationId}").SendAsync("ConversationStatusChanged", new
			{
				ConversationId = conversationId,
				Status = status,
				TimeStamp = DateTime.Now
			});
		}
	}
}