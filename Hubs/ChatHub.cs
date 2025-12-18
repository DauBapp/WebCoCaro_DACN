using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web_chơi_cờ_Caro.Data;
using Web_chơi_cờ_Caro.Models;

namespace Web_chơi_cờ_Caro.Hubs
{
	public class ChatHub : Hub
	{
		private readonly ApplicationDbContext _db;

		// userId -> list of connectionIds (hỗ trợ mở nhiều tab)
		private static readonly ConcurrentDictionary<string, List<string>> UserConnections = new();

		public ChatHub(ApplicationDbContext db)
		{
			_db = db;
		}

		// ============================
		//  USER CONNECT / DISCONNECT
		// ============================

		public override async Task OnConnectedAsync()
		{
			var userId = Context.UserIdentifier;
			if (!string.IsNullOrEmpty(userId))
			{
				AddConnection(userId, Context.ConnectionId);
				Console.WriteLine($"🟢 {userId} connected ({Context.ConnectionId})");
			}

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			var userId = Context.UserIdentifier;
			if (!string.IsNullOrEmpty(userId))
			{
				RemoveConnection(userId, Context.ConnectionId);
				Console.WriteLine($"🔴 {userId} disconnected ({Context.ConnectionId})");
			}

			await base.OnDisconnectedAsync(exception);
		}

		private void AddConnection(string userId, string connectionId)
		{
			UserConnections.AddOrUpdate(
				userId,
				_ => new List<string> { connectionId },
				(_, list) =>
				{
					lock (list)
						list.Add(connectionId);
					return list;
				});
		}

		private void RemoveConnection(string userId, string connectionId)
		{
			if (UserConnections.TryGetValue(userId, out var list))
			{
				lock (list)
				{
					list.Remove(connectionId);
					if (list.Count == 0)
					{
						UserConnections.TryRemove(userId, out _);
					}
				}
			}
		}


		// ============================
		//  SEND MESSAGE
		// ============================

		public async Task SendMessage(string receiverId, string message)
		{
			var senderId = Context.UserIdentifier;

			if (string.IsNullOrEmpty(senderId) ||
				string.IsNullOrEmpty(receiverId) ||
				string.IsNullOrWhiteSpace(message))
				return;

			// Kiểm tra người nhận có tồn tại không
			var receiverExists = await _db.Users.AnyAsync(u => u.Id == receiverId);
			if (!receiverExists)
			{
				await Clients.Caller.SendAsync("Error", "Người nhận không tồn tại.");
				return;
			}

			var utcNow = DateTime.UtcNow;

			var chat = new ChatMessage
			{
				SenderId = senderId,
				ReceiverId = receiverId,
				Message = message.Trim(),
				SentAt = utcNow,
				IsRead = false
			};

			_db.ChatMessages.Add(chat);
			await _db.SaveChangesAsync();

			// Payload chuẩn
			var msgPayload = new
			{
				senderId,
				receiverId,
				message = chat.Message,
				sentAt = utcNow.ToString("o")
			};

			// Gửi tới người nhận (tất cả tab đang mở)
			if (UserConnections.TryGetValue(receiverId, out var connections))
			{
				foreach (var connId in connections)
					await Clients.Client(connId).SendAsync("ReceiveMessage", msgPayload);
			}

			// Gửi lại cho người gửi (tất cả tab của sender)
			if (UserConnections.TryGetValue(senderId, out var senderConnections))
			{
				foreach (var connId in senderConnections)
					await Clients.Client(connId).SendAsync("ReceiveMessage", msgPayload);
			}
		}


		// ============================
		//  LOAD CHAT HISTORY
		// ============================

		public async Task LoadChatHistory(string friendId)
		{
			var userId = Context.UserIdentifier;
			if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(friendId))
				return;

			var messages = await _db.ChatMessages
				.Where(m =>
					(m.SenderId == userId && m.ReceiverId == friendId) ||
					(m.SenderId == friendId && m.ReceiverId == userId))
				.OrderBy(m => m.SentAt)
				.ToListAsync();

			// Đảm bảo format thời gian nhất quán với UTC (giống như khi gửi tin nhắn mới)
			// SQL Server lưu DateTime không có timezone, nên khi đọc lại Kind = Unspecified
			// Ta cần đảm bảo coi nó như UTC vì khi lưu ta đã dùng DateTime.UtcNow
			var history = messages.Select(m =>
			{
				DateTime utcSentAt;
				if (m.SentAt.Kind == DateTimeKind.Utc)
				{
					utcSentAt = m.SentAt;
				}
				else if (m.SentAt.Kind == DateTimeKind.Local)
				{
					utcSentAt = m.SentAt.ToUniversalTime();
				}
				else // Unspecified - SQL Server trả về thường là Unspecified
				{
					// Coi như đã là UTC vì khi lưu ta dùng DateTime.UtcNow
					utcSentAt = DateTime.SpecifyKind(m.SentAt, DateTimeKind.Utc);
				}

				return new
				{
					senderId = m.SenderId,
					receiverId = m.ReceiverId,
					message = m.Message,
					sentAt = utcSentAt.ToString("o") // Format ISO 8601 với UTC timezone
				};
			}).ToList();

			await Clients.Caller.SendAsync("ChatHistory", history);
		}
	}
}
