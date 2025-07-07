using Microsoft.AspNetCore.SignalR;

namespace XuanEmart.Hubs
{
	public class ChatHub : Hub
	{
		public async Task JoinConversation(string conversationId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation-{conversationId}");
		}

		public async Task LeaveConversation(string conversationId)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Conversation-{conversationId}");
		}
	}
}