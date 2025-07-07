using GenerativeAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XuanEmart.Models;
using XuanEmart.Repository;
using XuanEmart.Service;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IChatService _chatService;
    private readonly IConfiguration _configuration;

    public ChatController(DataContext context, IChatService chatService, IConfiguration configuration)
    {
        _context = context;
        _chatService = chatService;
        _configuration = configuration;
    }

    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessage([FromBody] SendChatMessageRequest request)
    {
        // Kiểm tra user
        var user = await _context.Set<AppUserModel>().FindAsync(request.UserId);
        var isGuest = user is null;

        // Tìm hoặc tạo conversation
        var conversation = await _context.Conversations.FirstOrDefaultAsync(x => x.UserId == request.UserId);

        // Thêm message
        var userMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderType = isGuest ? "guest" : "user",
            SenderId = request.UserId,
            Message = request.Message,
            CreatedAt = DateTime.Now,
            IsFromBot = false
        };
        _context.ChatMessages.Add(userMessage);
        await _context.SaveChangesAsync();

        // Gửi message qua SignalR
        await _chatService.SendMessage(conversation.Id.ToString(), request.Message, "user", request.UserId);

        if (!conversation.IsBotHandled)
        {
            return Ok(new ChatMessageDTO
            {
                ChatId = userMessage.Id,
                ConversationId = userMessage.ConversationId,
                SenderType = userMessage.SenderType,
                SenderId = userMessage.SenderId,
                Message = userMessage.Message,
                TimeStamp = userMessage.CreatedAt,
                IsFromBot = userMessage.IsFromBot ?? false
            });
        }

        // Lấy 5 tin nhắn gần nhất
        var recentMessages = await _context.ChatMessages
            .Where(x => x.ConversationId == conversation.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        var contextPrompt = string.Join("\n", recentMessages.Select(m => $"{m.SenderType}: {m.Message}"));

        // Gọi bot trả lời (giả lập, bạn có thể thay bằng logic thực tế)
        var botResponse = await GenerateBotResponse(contextPrompt);
        var botMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderType = "bot",
            Message = botResponse,
            CreatedAt = DateTime.Now,
            IsFromBot = true
        };
        _context.ChatMessages.Add(botMessage);
        await _context.SaveChangesAsync();

        await _chatService.SendMessage(conversation.Id.ToString(), botResponse, "bot", null);

        conversation.IsBotHandled = true;
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync();

        return Ok(new ChatMessageDTO
        {
            ChatId = userMessage.Id,
            ConversationId = userMessage.ConversationId,
            SenderType = userMessage.SenderType,
            SenderId = userMessage.SenderId,
            Message = userMessage.Message,
            TimeStamp = userMessage.CreatedAt,
            IsFromBot = userMessage.IsFromBot ?? false
        });
    }

    [HttpGet("get-conversation/{userId}")]
    public async Task<IActionResult> GetConversation(string userId)
    {
        // Tìm hội thoại
        var conversation = await _context.Conversations.FirstOrDefaultAsync(x => x.UserId == userId);
        if (conversation == null)
        {
            // Nếu chưa có thì tạo mới
            conversation = new Conversations
            {
                UserId = userId,
                Status = "pending",
                IsBotHandled = true,
                CreatedAt = DateTime.Now
            };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
        }

        // Nếu là guest thì không kiểm tra user
        bool isGuest = userId.StartsWith("guest_");
        string userName = "Khách";
        if (!isGuest)
        {
            var user = await _context.Set<AppUserModel>().FindAsync(userId);
            if (user == null)
                return NotFound(new { Message = "Không tìm thấy user" });
            userName = user.UserName;
        }

        var messages = await _context.ChatMessages
            .Where(x => x.ConversationId == conversation.Id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        var result = new ConversationDTO
        {
            ConversationId = conversation.Id,
            UserId = conversation.UserId,
            UserName = userName,
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
        };

        return Ok(result);
    }

    private async Task<string> GenerateBotResponse(string contextPrompt, CancellationToken cancellationToken = default)
    {
        try
        {
            if (contextPrompt.ToLower().Contains("flash sale") || 
                contextPrompt.ToLower().Contains("khuyến mãi") || 
                contextPrompt.ToLower().Contains("giảm giá"))
            {
                // TODO: Thay bằng logic thực tế nếu có
                return "Hiện tại chưa có chương trình Flash Sale.";
            }

            if (contextPrompt.ToLower().Contains("sản phẩm") || 
                contextPrompt.ToLower().Contains("giá") || 
                contextPrompt.ToLower().Contains("mua") ||
                contextPrompt.ToLower().Contains("có bán"))
            {
                // Lấy tất cả sản phẩm
                var allProducts = await _context.Products.ToListAsync();
                // Tìm sản phẩm phù hợp với nhu cầu (tên hoặc mô tả chứa từ khóa trong contextPrompt)
                var lowerPrompt = contextPrompt.ToLower();
                var matchedProducts = allProducts
                    .Where(p => lowerPrompt.Contains(p.Name.ToLower()) || lowerPrompt.Contains(p.Description.ToLower()))
                    .Take(3)
                    .ToList();

                if (matchedProducts.Any())
                {
                    var productList = string.Join("\n", matchedProducts.Select(p => $"- {p.Name} (Giá: {p.Price:N0}₫)"));
                    return $"Tôi gợi ý cho bạn các sản phẩm phù hợp:\n{productList}\nBạn muốn biết thêm về sản phẩm nào?";
                }
                else
                {
                    // Nếu không tìm thấy sản phẩm phù hợp, gợi ý 3 sản phẩm nổi bật
                    var suggestedProducts = allProducts.OrderByDescending(p => p.Price).Take(3).ToList();
                    if (suggestedProducts.Any())
                    {
                        var productList = string.Join("\n", suggestedProducts.Select(p => $"- {p.Name} (Giá: {p.Price:N0}₫)"));
                        return $"Bạn muốn hỏi về sản phẩm nào? Một số sản phẩm nổi bật:\n{productList}\nBạn quan tâm đến sản phẩm nào?";
                    }
                    else
                    {
                        return "Bạn muốn hỏi về sản phẩm nào?";
                    }
                }
            }

            if (contextPrompt.ToLower().Contains("đặt hàng") || 
                contextPrompt.ToLower().Contains("mua hàng") || 
                contextPrompt.ToLower().Contains("thanh toán") ||
                contextPrompt.ToLower().Contains("vận chuyển"))
            {
                // TODO: Thay bằng logic thực tế nếu có
                return "Bạn muốn biết về quy trình đặt hàng hay vận chuyển?";
            }

            if (contextPrompt.ToLower().Contains("đổi trả") || 
                contextPrompt.ToLower().Contains("hoàn tiền") || 
                contextPrompt.ToLower().Contains("bảo hành") ||
                contextPrompt.ToLower().Contains("khiếu nại"))
            {
                // TODO: Thay bằng logic thực tế nếu có
                return "Bạn cần hỗ trợ về đổi trả, hoàn tiền hay bảo hành?";
            }

            var apiKey = _configuration["Gemini:ApiKey"];
            var client = new GenerativeModel(model: "gemini-2.0-flash", apiKey: apiKey);

            var prompt = $@"Bạn là trợ lý ảo của Xuân Emart - cửa hàng bán đồng hồ chính hãng tại Việt Nam.

            Lịch sử hội thoại:
            {contextPrompt}

            Hướng dẫn trả lời:
            1. Trả lời ngắn gọn, rõ ràng và thân thiện
            2. Tập trung vào thông tin về sản phẩm, giá cả, chính sách bảo hành
            3. Nếu không chắc chắn, đề nghị chuyển tiếp cho nhân viên hỗ trợ
            4. Không trả lời các câu hỏi về chính trị, tôn giáo hoặc nội dung không phù hợp
            5. Luôn giữ thái độ chuyên nghiệp và lịch sự
            6. Nếu khách hàng hỏi về sản phẩm, hãy gợi ý các sản phẩm phù hợp
            7. Nếu có chương trình khuyến mãi, hãy thông báo cho khách hàng
            8. Nếu khách hàng hỏi về quy trình đặt hàng, hãy giải thích các bước đơn giản
            9. Nếu khách hàng hỏi về chính sách đổi trả, hãy giải thích rõ ràng và hướng dẫn họ liên hệ admin nếu cần

            Hãy trả lời câu hỏi của khách hàng một cách hữu ích nhất.";

            var response = await client.GenerateContentAsync(prompt, cancellationToken);

            return response.Text ?? "Xin lỗi, tôi không có câu trả lời phù hợp lúc này.";
        }
        catch (Exception)
        {
            return "Xin lỗi, tôi không thể trả lời lúc này.";
        }
    }
}

// Định nghĩa các model request/response nếu chưa có
public class SendChatMessageRequest
{
    public string UserId { get; set; }
    public string Message { get; set; }
}

public class ChatMessageDTO
{
    public int ChatId { get; set; }
    public int ConversationId { get; set; }
    public string SenderType { get; set; }
    public string? SenderId { get; set; }
    public string Message { get; set; }
    public DateTime TimeStamp { get; set; }
    public bool IsFromBot { get; set; }
}

// DTO cho hội thoại
public class ConversationDTO
{
    public int ConversationId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Status { get; set; }
    public bool IsBotHandled { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ChatMessageDTO> Messages { get; set; }
}

// Model cho request admin reply
public class AdminReplyRequest
{
    public int ConversationId { get; set; }
    public string AdminId { get; set; }
    public string Message { get; set; }
}

public class ToggleBotRequest
{
    public int ConversationId { get; set; }
    public bool IsBotHandled { get; set; }
}
