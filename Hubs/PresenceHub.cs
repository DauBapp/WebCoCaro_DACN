using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Web_chơi_cờ_Caro.Data;
using Web_chơi_cờ_Caro.Models;

namespace Web_chơi_cờ_Caro.Hubs
{
	public class NameUserIdProvider : IUserIdProvider
	{
		public string? GetUserId(HubConnectionContext connection)
		{
			// ✅ Lấy ID thực từ ClaimTypes.NameIdentifier (khớp AspNetUsers.Id)
			return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		}
	}

	public class PresenceHub : Hub
	{
		private readonly ApplicationDbContext _db;
		// ✅ Hỗ trợ multiple connections per user (nhiều tab)
		private static ConcurrentDictionary<string, HashSet<string>> OnlineUsers = new(); // userId -> Set<connectionId>

		public PresenceHub(ApplicationDbContext db)
		{
			_db = db;
		}

		public override async Task OnConnectedAsync()
		{
			var userId = Context.UserIdentifier;
			if (string.IsNullOrEmpty(userId))
			{
				await base.OnConnectedAsync();
				return;
			}

			// ✅ Lưu connectionId vào danh sách người dùng online (hỗ trợ nhiều tab)
			var connections = OnlineUsers.GetOrAdd(userId, _ => new HashSet<string>());
			lock (connections)
			{
				connections.Add(Context.ConnectionId);
			}

			// Thêm user vào group để có thể gửi thông báo
			await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

			var user = await _db.Users.FindAsync(userId);
			var wasOffline = user?.Status != "Online";
			
			if (user != null)
			{
				user.Status = "Online";
				user.LastLoginTime = DateTime.Now;
				user.LastActive = DateTime.Now;
				await _db.SaveChangesAsync();
			}

			// ✅ Lấy danh sách bạn bè
			var friends = await _db.Friends
				.Where(f => f.UserId == userId)
				.Include(f => f.FriendUser)
				.Select(f => new
				{
					id = f.FriendUser.Id,
					name = f.FriendUser.UserName,
					avatarUrl = string.IsNullOrWhiteSpace(f.FriendUser.AvatarUrl) 
						? "/images/default-avatar.png" 
						: f.FriendUser.AvatarUrl,
					status = OnlineUsers.ContainsKey(f.FriendUser.Id) ? "Online" : f.FriendUser.Status ?? "offline",
					lastActive = f.FriendUser.LastLoginTime
				})
				.ToListAsync();

			// ✅ Gửi danh sách bạn bè cho chính user này
			await Clients.Caller.SendAsync("ActiveFriendsList", friends);

			// ✅ Chỉ gửi thông báo "online" cho bạn bè nếu user vừa mới online (không phải đã online từ trước)
			if (wasOffline && user != null)
			{
				foreach (var friend in friends)
				{
					if (OnlineUsers.TryGetValue(friend.id, out var friendConnections))
					{
						// Gửi cho tất cả các connection của bạn bè
						foreach (var connId in friendConnections)
						{
							await Clients.Client(connId).SendAsync("PresenceUpdated", new
							{
								id = user.Id,
								name = user.UserName,
								avatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) 
									? "/images/default-avatar.png" 
									: user.AvatarUrl,
								status = "Online",
								lastActive = DateTime.Now
							});
						}
					}
				}
			}

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			var userId = Context.UserIdentifier;
			if (string.IsNullOrEmpty(userId))
			{
				await base.OnDisconnectedAsync(exception);
				return;
			}

			// ✅ Xóa connectionId khỏi danh sách (hỗ trợ nhiều tab)
			bool isNowOffline = false;
			if (OnlineUsers.TryGetValue(userId, out var connections))
			{
				lock (connections)
				{
					connections.Remove(Context.ConnectionId);
					// Nếu không còn connection nào thì user thực sự offline
					if (connections.Count == 0)
					{
						OnlineUsers.TryRemove(userId, out _);
						isNowOffline = true;
					}
				}
			}

			// Xóa user khỏi group
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

			var user = await _db.Users.FindAsync(userId);
			if (user != null && isNowOffline)
			{
				user.Status = "Offline";
				user.LastActive = DateTime.Now;
				await _db.SaveChangesAsync();
			}

			// ✅ Chỉ gửi sự kiện "offline" cho bạn bè nếu user thực sự offline (không còn tab nào)
			if (isNowOffline && user != null)
			{
				var friends = await _db.Friends
					.Where(f => f.UserId == userId)
					.Include(f => f.FriendUser)
					.ToListAsync();

				foreach (var friend in friends)
				{
					if (OnlineUsers.TryGetValue(friend.FriendId, out var friendConnections))
					{
						// Gửi cho tất cả các connection của bạn bè
						foreach (var connId in friendConnections)
						{
							await Clients.Client(connId).SendAsync("PresenceUpdated", new
							{
								id = user.Id,
								name = user.UserName,
								avatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) 
									? "/images/default-avatar.png" 
									: user.AvatarUrl,
								status = "offline",
								lastActive = user.LastActive ?? user.LastLoginTime
							});
						}
					}
				}
			}

			await base.OnDisconnectedAsync(exception);
		}

		// ✅ Cho phép client gọi để lấy lại danh sách bạn bè thủ công
		public async Task GetActiveFriends()
		{
			var userId = Context.UserIdentifier;
			if (string.IsNullOrEmpty(userId)) return;

			var friends = await _db.Friends
				.Where(f => f.UserId == userId)
				.Include(f => f.FriendUser)
				.Select(f => new
				{
					id = f.FriendUser.Id,
					name = f.FriendUser.UserName,
					avatarUrl = string.IsNullOrWhiteSpace(f.FriendUser.AvatarUrl) 
						? "/images/default-avatar.png" 
						: f.FriendUser.AvatarUrl,
					status = OnlineUsers.ContainsKey(f.FriendUser.Id) ? "Online" : f.FriendUser.Status ?? "offline",
					lastActive = f.FriendUser.LastLoginTime
				})
				.ToListAsync();

			await Clients.Caller.SendAsync("ActiveFriendsList", friends);
		}

		// ✅ Gửi lời mời kết bạn
		public async Task SendFriendRequest(string receiverId)
		{
			var senderId = Context.UserIdentifier;
			if (string.IsNullOrEmpty(senderId) || senderId == receiverId)
			{
				await Clients.Caller.SendAsync("FriendRequestFailed", "Người nhận không hợp lệ.");
				return;
			}

			try
			{
				// 1️⃣ Kiểm tra nếu đã là bạn bè
				bool alreadyFriends = await _db.Friends
					.AnyAsync(f =>
						(f.UserId == senderId && f.FriendId == receiverId) ||
						(f.UserId == receiverId && f.FriendId == senderId));

				if (alreadyFriends)
				{
					await Clients.Caller.SendAsync("FriendRequestFailed", "Bạn và người này đã là bạn bè.");
					return;
				}

				// 2️⃣ Kiểm tra nếu đã có yêu cầu kết bạn đang chờ
				bool alreadyPending = await _db.FriendRequests
					.AnyAsync(r =>
						((r.SenderId == senderId && r.ReceiverId == receiverId) ||
						 (r.SenderId == receiverId && r.ReceiverId == senderId))
						&& r.Status == "Pending");

				if (alreadyPending)
				{
					await Clients.Caller.SendAsync("FriendRequestFailed", "Đã có lời mời kết bạn đang chờ xác nhận.");
					return;
				}

				// 3️⃣ Nếu hợp lệ -> tạo yêu cầu kết bạn mới
				var request = new FriendRequest
				{
					SenderId = senderId,
					ReceiverId = receiverId,
					Status = "Pending",
					SentAt = DateTime.Now
				};

				_db.FriendRequests.Add(request);
				await _db.SaveChangesAsync();

				// 4️⃣ Gửi realtime cho người nhận nếu họ đang online (gửi cho tất cả các tab)
				if (OnlineUsers.TryGetValue(receiverId, out var receiverConnections))
				{
					var sender = await _db.Users.FindAsync(senderId);
					if (sender != null)
					{
						foreach (var connId in receiverConnections)
						{
							await Clients.Client(connId).SendAsync("FriendRequestReceived", new
							{
								id = sender.Id,
								name = sender.UserName
							});
						}
					}
				}

				// 5️⃣ Gửi phản hồi thành công cho người gửi
				await Clients.Caller.SendAsync("FriendRequestSent", receiverId);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error in SendFriendRequest: {ex.Message}");
				await Clients.Caller.SendAsync("FriendRequestFailed", "Đã xảy ra lỗi, vui lòng thử lại sau.");
			}
		}


		// ✅ Người nhận chấp nhận hoặc từ chối
		public async Task RespondFriendRequest(string senderId, bool accept)
		{
			var receiverId = Context.UserIdentifier;
			if (string.IsNullOrEmpty(receiverId)) return;

			var request = await _db.FriendRequests
				.FirstOrDefaultAsync(r =>
					r.SenderId == senderId &&
					r.ReceiverId == receiverId &&
					r.Status == "Pending");

			if (request == null) return;

			request.Status = accept ? "Accepted" : "Rejected";
			await _db.SaveChangesAsync();

			if (accept)
			{
				// ✅ Thêm vào bảng Friends (2 chiều)
				_db.Friends.Add(new Friend { UserId = receiverId, FriendId = senderId });
				_db.Friends.Add(new Friend { UserId = senderId, FriendId = receiverId });
				await _db.SaveChangesAsync();
			}

			// ✅ Gửi thông báo real-time cho người gửi (gửi cho tất cả các tab)
			if (OnlineUsers.TryGetValue(senderId, out var senderConnections))
			{
				foreach (var connId in senderConnections)
				{
					await Clients.Client(connId).SendAsync("FriendRequestResponse", new
					{
						id = receiverId,
						status = accept ? "Accepted" : "Rejected"
					});
				}
			}

			// ✅ Gửi cập nhật mới cho chính người nhận (để xoá lời mời khỏi danh sách)
			await GetPendingFriendRequests();

			// ✅ Cập nhật lại danh sách bạn bè cho cả hai bên nếu đồng ý
			if (accept)
			{
				await GetActiveFriends(); // cho người nhận
				// ✅ Tái sử dụng senderConnections đã lấy ở trên
				if (senderConnections != null)
				{
					// Gửi refresh cho tất cả các tab của người gửi
					foreach (var connId in senderConnections)
					{
						await Clients.Client(connId).SendAsync("RefreshFriends");
					}
				}
			}
		}

		// ✅ Lấy danh sách lời mời kết bạn
		public async Task GetPendingFriendRequests()
		{
			try
			{
				var userId = Context.UserIdentifier;
				if (string.IsNullOrEmpty(userId)) return;

				var pending = await _db.FriendRequests
					.Where(r => r.ReceiverId == userId && r.Status == "Pending")
					.Join(_db.Users,
						  r => r.SenderId,
						  u => u.Id,
						  (r, u) => new
						  {
							  id = u.Id,
							  name = u.UserName,
							  sentAt = r.SentAt
						  })
					.ToListAsync();

				await Clients.Caller.SendAsync("PendingFriendRequests", pending);
			}
			catch (Exception ex)
			{
				Console.WriteLine("❌ [PresenceHub] Lỗi GetPendingFriendRequests: " + ex.Message);
				Console.WriteLine(ex.StackTrace);
			}
		}
	}
}
