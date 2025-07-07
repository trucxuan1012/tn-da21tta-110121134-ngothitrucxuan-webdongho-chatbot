using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XuanEmart.Models;
using XuanEmart.Repository;
using XuanEmart.Service;

namespace XuanEMart.Areas.Admin.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [AllowAnonymous]
    public class ChatApiController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IChatService _chatService;
        private readonly IConfiguration _configuration;

        public ChatApiController(DataContext context, IChatService chatService, IConfiguration configuration)
        {
            _context = context;
            _chatService = chatService;
            _configuration = configuration;
        }

        [HttpPost("admin-reply")]
        public async Task<IActionResult> AdminReply([FromBody] AdminReplyRequest request)
        {
            var conversation = await _context.Conversations.FindAsync(request.ConversationId);
            if (conversation == null)
                return NotFound(new { Message = "Không tìm thấy hội thoại" });

            var message = new ChatMessage
            {
                ConversationId = request.ConversationId,
                SenderId = request.AdminId,
                Message = request.Message,
                CreatedAt = DateTime.Now,
                IsFromBot = false,
                SenderType = "admin"
            };
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            await _chatService.SendMessage(conversation.Id.ToString(), message.Message, message.SenderType, message.SenderId);

            return Ok(new ChatMessageDTO
            {
                ChatId = message.Id,
                ConversationId = message.ConversationId,
                SenderType = message.SenderType,
                SenderId = message.SenderId,
                Message = message.Message,
                TimeStamp = message.CreatedAt,
                IsFromBot = message.IsFromBot ?? false
            });
        }

        [HttpGet("pending-conversations")]
        public async Task<IActionResult> GetPendingConversations()
        {
            // Xóa các hội thoại guest quá 24h
            var now = DateTime.Now;
            var expiredGuestConversations = await _context.Conversations
                .Where(x => x.UserId.StartsWith("guest_") && x.CreatedAt < now.AddHours(-24))
                .ToListAsync();
            if (expiredGuestConversations.Any())
            {
                var expiredIds = expiredGuestConversations.Select(x => x.Id).ToList();
                var expiredMessages = await _context.ChatMessages
                    .Where(m => expiredIds.Contains(m.ConversationId))
                    .ToListAsync();
                _context.ChatMessages.RemoveRange(expiredMessages);
                _context.Conversations.RemoveRange(expiredGuestConversations);
                await _context.SaveChangesAsync();
            }

            var conversations = await _context.Conversations
                .Where(x => x.Status == "pending")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var userIds = conversations.Select(x => x.UserId).Distinct().ToList();
            var users = await _context.Set<AppUserModel>()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var result = new List<ConversationDTO>();
            foreach (var conversation in conversations)
            {
                var messages = await _context.ChatMessages
                    .Where(x => x.ConversationId == conversation.Id)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();

                users.TryGetValue(conversation.UserId, out var user);

                result.Add(new ConversationDTO
                {
                    ConversationId = conversation.Id,
                    UserId = conversation.UserId,
                    UserName = user?.UserName ?? string.Empty,
                    Status = conversation.Status,
                    IsBotHandled = conversation.IsBotHandled,
                    CreatedAt = conversation.CreatedAt,
                    Messages = messages.Select(m => new ChatMessageDTO
                    {
                        ChatId = m.Id,
                        ConversationId = m.ConversationId,
                        SenderType = m.SenderType,
                        SenderId = m.SenderId,
                        Message = m.Message,
                        TimeStamp = m.CreatedAt,
                        IsFromBot = m.IsFromBot ?? false
                    }).ToList()
                });
            }
            return Ok(result);
        }

        [HttpPost("toggle-bot")]
        public async Task<IActionResult> ToggleBot([FromBody] ToggleBotRequest request)
        {
            var conversation = await _context.Conversations.FindAsync(request.ConversationId);
            if (conversation == null)
                return NotFound(new { Message = "Không tìm thấy hội thoại" });

            conversation.IsBotHandled = request.IsBotHandled;
            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Đã {(request.IsBotHandled ? "bật" : "tắt")} bot cho hội thoại." });
        }
    }

    // Controller MVC cho giao diện quản lý chatbot
    [Area("Admin")]
    public class ChatController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}