using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Web_chơi_cờ_Caro.Data;

namespace Web_chơi_cờ_Caro.Hubs
{
	public class GameHub : Hub
	{
		// Lưu trữ thông tin phòng và người chơi
		private static readonly ConcurrentDictionary<string, RoomInfo> _rooms = new();
		private static readonly ConcurrentDictionary<string, string> _userRooms = new(); // connectionId -> roomId

		private readonly ApplicationDbContext _db;
		public GameHub(ApplicationDbContext db)
		{
			_db = db;
		}



		public class RoomInfo
		{
			public string RoomId { get; set; } = "";
			public string RoomName { get; set; } = "";
			public string HostId { get; set; } = "";
			public string HostName { get; set; } = "";
			public List<PlayerInfo> Players { get; set; } = new();
			public GameState GameState { get; set; } = new();
			public int MaxPlayers { get; set; } = 2;
			public bool IsPrivate { get; set; } = false;
			public DateTime CreatedAt { get; set; } = DateTime.Now;
		}

		public class PlayerInfo
		{
			public string ConnectionId { get; set; } = "";
			public string Name { get; set; } = "";
			public string Symbol { get; set; } = ""; // X hoặc O
			public bool IsHost { get; set; } = false;
		}

		public class GameState
		{
			public bool IsStarted { get; set; } = false;
			public string CurrentPlayer { get; set; } = "X";
			public int[][] Board { get; set; } = CreateEmptyBoard(); // 0 = empty, 1 = X, 2 = O
			public int TimeLeft { get; set; } = 60;
			public string Winner { get; set; } = "";

			private static int[][] CreateEmptyBoard()
			{
				var board = new int[20][];
				for (int i = 0; i < 20; i++)
				{
					board[i] = new int[20];
				}
				return board;
			}

			public void Reset()
			{
				IsStarted = false;
				CurrentPlayer = "X";
				Board = CreateEmptyBoard();
				TimeLeft = 60;
				Winner = "";
			}

		}

		// Test connection method (used by client to verify hub methods)
		public async Task TestConnection(string roomId)
		{
			var connectionId = Context.ConnectionId;
			var player = _rooms.TryGetValue(roomId, out var room)
				? room.Players.FirstOrDefault(p => p.ConnectionId == connectionId)
				: null;

			await Clients.Caller.SendAsync(
				"TestConnectionResult",
				$"Connection ID: {connectionId}",
				$"Room ID: {roomId}",
				$"Player: {player?.Name ?? "Unknown"}",
				$"Is Host: {player?.IsHost ?? false}"
			);
		}


		// Kiểm tra phòng có tồn tại không
		public async Task CheckRoomExists(string roomId)
		{
			var exists = _rooms.TryGetValue(roomId, out var room);
			int playerCount = exists ? room!.Players.Count : 0;
			int maxPlayers = exists ? room!.MaxPlayers : 2;

			await Clients.Caller.SendAsync("RoomExistsResult", roomId, exists, playerCount, maxPlayers);
		}

		// Tham gia phòng
		public async Task JoinRoom(string roomId, string playerName)
		{
			var connectionId = Context.ConnectionId;

			Console.WriteLine($"=== JOIN ROOM REQUEST ===");
			Console.WriteLine($"Room ID: {roomId}");
			Console.WriteLine($"Player Name: {playerName}");
			Console.WriteLine($"Connection ID: {connectionId}");

			// Kiểm tra xem người chơi đã ở phòng nào chưa
			if (_userRooms.TryGetValue(connectionId, out var currentRoom))
			{
				Console.WriteLine($"Player already in room {currentRoom}, leaving first...");
				await LeaveRoom(currentRoom);
			}

			// Tạo phòng mới nếu chưa tồn tại
			if (!_rooms.TryGetValue(roomId, out var room))
			{
				room = new RoomInfo
				{
					RoomId = roomId,
					RoomName = $"Phòng {roomId}",
					HostId = connectionId,
					HostName = playerName,
					MaxPlayers = 2
				};
				_rooms.TryAdd(roomId, room);
				Console.WriteLine($"Created new room: {roomId} by {playerName}");
			}
			else
			{
				Console.WriteLine($"Joined existing room: {roomId} by {playerName}");
				Console.WriteLine($"Current players in room: {room.Players.Count}");
			}

			// Kiểm tra xem phòng có đầy không
			if (room.Players.Count >= room.MaxPlayers)
			{
				Console.WriteLine($"Room {roomId} is full!");
				await Clients.Caller.SendAsync("RoomFull", roomId);
				return;
			}

			// Thêm người chơi vào phòng
			var player = new PlayerInfo
			{
				ConnectionId = connectionId,
				Name = playerName,
				Symbol = room.Players.Count == 0 ? "X" : "O",
				IsHost = room.Players.Count == 0
			};

			room.Players.Add(player);
			_userRooms.TryAdd(connectionId, roomId);

			Console.WriteLine($"Added player {playerName} to room {roomId}");
			Console.WriteLine($"Total rooms: {_rooms.Count}");
			Console.WriteLine($"Total players in room {roomId}: {room.Players.Count}");
			Console.WriteLine($"Player is host: {player.IsHost}");
			Console.WriteLine($"Player symbol: {player.Symbol}");

			// Tham gia group SignalR
			await Groups.AddToGroupAsync(connectionId, roomId);
			Console.WriteLine($"Added {playerName} to SignalR group {roomId}");

			// Thông báo cho tất cả người chơi trong phòng
			await Clients.Group(roomId).SendAsync("playerJoined", player.Name, player.Symbol, room.Players.Count);
			Console.WriteLine($"Sent PlayerJoined event to group {roomId}");

			// Gửi lại danh sách người chơi cập nhật cho tất cả trong phòng
			await Clients.Group(roomId).SendAsync("playerListUpdated", room.Players);
			Console.WriteLine($"Sent PlayerListUpdated event to group {roomId}");

			// Gửi thông tin phòng cho người chơi mới
			await Clients.Caller.SendAsync("roomJoined", room.RoomId, room.RoomName, room.Players, room.GameState);
			Console.WriteLine($"Sent RoomJoined event to {playerName}");
			Console.WriteLine($"Room players: {string.Join(", ", room.Players.Select(p => $"{p.Name}({p.Symbol})"))}");

			// Cập nhật danh sách phòng cho tất cả
			await BroadcastRoomList();
			Console.WriteLine($"=== JOIN ROOM COMPLETED ===");
		}

		// Rời phòng
		public async Task LeaveRoom(string roomId)
		{
			var connectionId = Context.ConnectionId;

			if (_rooms.TryGetValue(roomId, out var room))
			{
				var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
				if (player != null)
				{
					room.Players.Remove(player);

					// Nếu chủ phòng rời đi, chuyển quyền cho người chơi khác
					if (player.IsHost && room.Players.Count > 0)
					{
						var newHost = room.Players.First();
						newHost.IsHost = true;
						room.HostId = newHost.ConnectionId;
						room.HostName = newHost.Name;
					}

					// Nếu không còn ai trong phòng, xóa phòng
					if (room.Players.Count == 0)
					{
						_rooms.TryRemove(roomId, out _);
					}
					else
					{
						// Thông báo cho người chơi khác
						await Clients.Group(roomId).SendAsync("playerLeft", player.Name, room.Players.Count);

						// Gửi lại danh sách người chơi cập nhật cho tất cả trong phòng
						await Clients.Group(roomId).SendAsync("playerListUpdated", room.Players);
						Console.WriteLine($"Sent PlayerListUpdated event after player left to group {roomId}");
					}
				}
			}

			_userRooms.TryRemove(connectionId, out _);
			await Groups.RemoveFromGroupAsync(connectionId, roomId);

			// Cập nhật danh sách phòng
			await BroadcastRoomList();
		}

		// Bắt đầu game
		public async Task StartGame(string roomId)
		{
			Console.WriteLine($"=== START GAME REQUEST ===");
			Console.WriteLine($"Room ID: {roomId}");
			Console.WriteLine($"Connection ID: {Context.ConnectionId}");

			if (_rooms.TryGetValue(roomId, out var room))
			{
				Console.WriteLine($"Room found, players count: {room.Players.Count}");
				Console.WriteLine($"Room players: {string.Join(", ", room.Players.Select(p => $"{p.Name}({p.Symbol})"))}");

				var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
				Console.WriteLine($"Player found: {player?.Name}, IsHost: {player?.IsHost}");

				if (player?.IsHost == true)
				{
					Console.WriteLine("Starting game...");
					room.GameState.IsStarted = true;
					room.GameState.CurrentPlayer = "X";
					room.GameState.TimeLeft = 60;
					room.GameState.Winner = "";

					await Clients.Group(roomId).SendAsync("gameStarted", room.GameState);
					Console.WriteLine("Game started successfully");
					Console.WriteLine($"Sent GameStarted event to group {roomId}");
				}
				else
				{
					Console.WriteLine("Player is not host, cannot start game");
					await Clients.Caller.SendAsync("Error", "Chỉ chủ phòng mới có thể bắt đầu game!");
				}
			}
			else
			{
				Console.WriteLine($"Room {roomId} not found");
				await Clients.Caller.SendAsync("Error", "Không tìm thấy phòng!");
			}

			Console.WriteLine($"=== START GAME COMPLETED ===");
		}

		// Thực hiện nước đi
		private static readonly ConcurrentDictionary<string, SemaphoreSlim> _roomSemaphores = new();

		public async Task MakeMove(string roomId, int row, int col)
		{
			Console.WriteLine($"=== MAKE MOVE REQUEST ===");
			Console.WriteLine($"Room ID: {roomId}");
			Console.WriteLine($"Row: {row}, Col: {col}");
			Console.WriteLine($"Connection ID: {Context.ConnectionId}");

			// Ensure we have a semaphore for this room to avoid race conditions
			var sem = _roomSemaphores.GetOrAdd(roomId, _ => new SemaphoreSlim(1, 1));
			await sem.WaitAsync();
			try
			{
				if (!_rooms.TryGetValue(roomId, out var room))
				{
					Console.WriteLine($"Room {roomId} not found");
					await Clients.Caller.SendAsync("Error", "Không tìm thấy phòng!");
					return;
				}

				Console.WriteLine($"Room found, game started: {room.GameState.IsStarted}");
				Console.WriteLine($"Current player: {room.GameState.CurrentPlayer}");
				Console.WriteLine($"Winner: {room.GameState.Winner}");

				var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
				Console.WriteLine($"Player found: {player?.Name}, Symbol: {player?.Symbol}");

				if (player == null || !room.GameState.IsStarted || !string.IsNullOrEmpty(room.GameState.Winner))
				{
					Console.WriteLine($"Cannot make move. Player: {player?.Name}, Game started: {room.GameState.IsStarted}, Winner: {room.GameState.Winner}");
					await Clients.Caller.SendAsync("Error", "Không thể thực hiện nước đi!");
					return;
				}

				// Kiểm tra lượt đi
				if (room.GameState.CurrentPlayer != player.Symbol)
				{
					Console.WriteLine($"Not player's turn. Current: {room.GameState.CurrentPlayer}, Player: {player.Symbol}");
					await Clients.Caller.SendAsync("Error", "Chưa đến lượt của bạn!");
					return;
				}

				// Validate coordinates
				if (row < 0 || row >= room.GameState.Board.Length || col < 0 || col >= room.GameState.Board[0].Length)
				{
					Console.WriteLine($"Position ({row}, {col}) is out of range");
					await Clients.Caller.SendAsync("Error", "Nước đi không hợp lệ!");
					return;
				}

				// Validate empty cell
				if (room.GameState.Board[row][col] != 0)
				{
					Console.WriteLine($"Position ({row}, {col}) is already occupied");
					await Clients.Caller.SendAsync("Error", "Ô đã được đánh!");
					return;
				}

				try
				{
					Console.WriteLine($"Position ({row}, {col}) is empty, making move");

					// Đặt quân cờ
					room.GameState.Board[row][col] = player.Symbol == "X" ? 1 : 2;

					// Lấy hoặc tạo lịch sử trận đấu (async)
					var gameHistory = await _db.GameHistories
						.FirstOrDefaultAsync(g => g.RoomId == roomId && g.EndedAt == null);

					if (gameHistory == null)
					{
						gameHistory = new GameHistory
						{
							RoomId = roomId,
							PlayerXId = room.Players.FirstOrDefault(p => p.Symbol == "X")?.ConnectionId ?? "",
							PlayerOId = room.Players.FirstOrDefault(p => p.Symbol == "O")?.ConnectionId ?? "",
							StartedAt = DateTime.Now
						};
						_db.GameHistories.Add(gameHistory);
						await _db.SaveChangesAsync(); // ensure Id is generated
						Console.WriteLine($"Created new GameHistory (Id: {gameHistory.Id}) for room {roomId}");
					}

					// Ghi nhận nước đi
					var move = new MoveRecord
					{
						GameHistoryId = gameHistory.Id,
						PlayerSymbol = player.Symbol,
						Row = row,
						Col = col,
						MoveTime = DateTime.Now
					};
					try
					{
						_db.MoveRecords.Add(move);
						await _db.SaveChangesAsync();
					}
					catch (Exception ex)
					{
						Console.WriteLine("Lỗi khi lưu nước đi: " + ex.Message);
						if (ex.InnerException != null)
							Console.WriteLine("Chi tiết SQL: " + ex.InnerException.Message);
					}

					//_db.MoveRecords.Add(move);
					//await _db.SaveChangesAsync();
					Console.WriteLine($"Saved MoveRecord for GameHistoryId {gameHistory.Id} at ({row},{col}) by {player.Symbol}");

					// Kiểm tra thắng
					if (CheckWin(room.GameState.Board, row, col, room.GameState.Board[row][col]))
					{
						room.GameState.Winner = player.Symbol;
						Console.WriteLine($"Player {player.Name} wins!");
						gameHistory.EndedAt = DateTime.Now;
						gameHistory.Winner = player.Symbol;
						_db.GameHistories.Update(gameHistory);
						await _db.SaveChangesAsync();
						await Clients.Group(roomId).SendAsync("gameEnded", player.Symbol, room.GameState);
						return;
					}

					// Kiểm tra hòa
					if (IsBoardFull(room.GameState.Board))
					{
						room.GameState.Winner = "Draw";
						Console.WriteLine("Game is a draw!");
						gameHistory.EndedAt = DateTime.Now;
						gameHistory.Winner = "Draw";
						_db.GameHistories.Update(gameHistory);
						await _db.SaveChangesAsync();
						await Clients.Group(roomId).SendAsync("gameEnded", "Draw", room.GameState);
						return;
					}

					// Chuyển lượt
					room.GameState.CurrentPlayer = room.GameState.CurrentPlayer == "X" ? "O" : "X";
					room.GameState.TimeLeft = 60;

					// Gửi thông tin nước đi cho tất cả
					await Clients.Group(roomId).SendAsync("moveMade", row, col, player.Symbol, room.GameState);
					Console.WriteLine($"Sent MoveMade event to group {roomId}");
				}
				catch (Exception ex)
				{
					// Catch DB or unexpected exceptions for this move
					Console.WriteLine("Exception while recording move: " + ex);
					await Clients.Caller.SendAsync("Error", "Lỗi khi lưu nước đi: " + ex.Message);
				}
			}
			finally
			{
				sem.Release();
				Console.WriteLine($"=== MAKE MOVE COMPLETED ===");
			}
		}

		//Reset game
		public async Task ResetGame(string roomId)
		{
			if (!_rooms.TryGetValue(roomId, out var room))
			{
				await Clients.Caller.SendAsync("Error", "Room not found.");
				return;
			}

			var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
			if (player == null)
			{
				await Clients.Caller.SendAsync("Error", "You are not in this room.");
				return;
			}

			if (!player.IsHost)
			{
				await Clients.Caller.SendAsync("Error", "Only host can reset the game.");
				return;
			}

			var ongoingGame = await _db.GameHistories
			.FirstOrDefaultAsync(g => g.RoomId == roomId && g.EndedAt == null);

			if (ongoingGame != null)
			{
				ongoingGame.EndedAt = DateTime.Now;
				ongoingGame.Winner = "Reset"; // hoặc "Aborted"
				_db.GameHistories.Update(ongoingGame);
				await _db.SaveChangesAsync();
			}

			// Sử dụng Reset() thay vì new GameState()
			room.GameState.Reset();
			await Clients.Group(roomId).SendAsync("gameReset", room.GameState);
		}

		// Gửi tin nhắn chat
		public async Task SendMessage(string roomId, string message)
		{
			var player = _rooms.TryGetValue(roomId, out var room)
				? room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId)
				: null;

			if (player != null)
			{
				await Clients.Group(roomId).SendAsync("messageReceived", player.Name, message);
			}
		}

		// Lấy danh sách phòng
		public async Task GetRoomList()
		{
			var roomList = _rooms.Values.Select(r => new
			{
				r.RoomId,
				r.RoomName,
				r.HostName,
				Players = r.Players.Count,
				MaxPlayers = r.MaxPlayers,
				r.IsPrivate,
				Status = r.GameState.IsStarted ? "playing" : "waiting"
			}).ToList();

			await Clients.Caller.SendAsync("RoomListUpdated", roomList);
		}

		// Broadcast danh sách phòng
		private async Task BroadcastRoomList()
		{
			var roomList = _rooms.Values.Select(r => new
			{
				r.RoomId,
				r.RoomName,
				r.HostName,
				Players = r.Players.Count,
				MaxPlayers = r.MaxPlayers,
				r.IsPrivate,
				Status = r.GameState.IsStarted ? "playing" : "waiting"
			}).ToList();

			await Clients.All.SendAsync("RoomListUpdated", roomList);
		}

		// Kiểm tra thắng
		// Kiểm tra thắng
		private bool CheckWin(int[][] board, int row, int col, int player)
		{
			int rows = board.Length;
			int cols = board[0].Length;
			int count;

			// Kiểm tra hàng ngang
			count = 1;
			for (int i = col - 1; i >= 0 && board[row][i] == player; i--) count++;
			for (int i = col + 1; i < cols && board[row][i] == player; i++) count++;
			if (count >= 5) return true;

			// Kiểm tra hàng dọc
			count = 1;
			for (int i = row - 1; i >= 0 && board[i][col] == player; i--) count++;
			for (int i = row + 1; i < rows && board[i][col] == player; i++) count++;
			if (count >= 5) return true;

			// Kiểm tra đường chéo chính
			count = 1;
			for (int i = 1; row - i >= 0 && col - i >= 0 && board[row - i][col - i] == player; i++) count++;
			for (int i = 1; row + i < rows && col + i < cols && board[row + i][col + i] == player; i++) count++;
			if (count >= 5) return true;

			// Kiểm tra đường chéo phụ
			count = 1;
			for (int i = 1; row - i >= 0 && col + i < cols && board[row - i][col + i] == player; i++) count++;
			for (int i = 1; row + i < rows && col - i >= 0 && board[row + i][col - i] == player; i++) count++;
			if (count >= 5) return true;

			return false;
		}

		// Kiểm tra bàn cờ đầy
		private bool IsBoardFull(int[][] board)
		{
			int rows = board.Length;
			int cols = board[0].Length;
			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < cols; j++)
				{
					if (board[i][j] == 0) return false;
				}
			}
			return true;
		}
		// Khi người chơi disconnect
		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			var connectionId = Context.ConnectionId;

			if (_userRooms.TryGetValue(connectionId, out var roomId))
			{
				await LeaveRoom(roomId);
			}

			await base.OnDisconnectedAsync(exception);
		}

		// Request match history and moves for a room
		public async Task RequestHistory(string roomId)
		{
			try
			{
				// Load histories for this room (most recent first)
				var histories = await _db.GameHistories
					.Where(g => g.RoomId == roomId)
					.OrderByDescending(g => g.StartedAt)
					.ToListAsync();

				var historyIds = histories.Select(h => h.Id).ToList();

				// Load moves for those histories
				var moves = await _db.MoveRecords
					.Where(m => historyIds.Contains(m.GameHistoryId))
					.OrderBy(m => m.MoveTime)
					.ToListAsync();

				// Map to lightweight DTOs to send to client
				var historyDtos = histories.Select(h => new
				{
					h.Id,
					h.RoomId,
					// Lấy tên người chơi từ _rooms nếu có, nếu không thì để ConnectionId
					PlayerX = _rooms.TryGetValue(h.RoomId, out var roomX) ?
						roomX.Players.FirstOrDefault(p => p.ConnectionId == h.PlayerXId)?.Name ?? h.PlayerXId
						: h.PlayerXId,
					PlayerO = _rooms.TryGetValue(h.RoomId, out var roomO) ?
						roomO.Players.FirstOrDefault(p => p.ConnectionId == h.PlayerOId)?.Name ?? h.PlayerOId
						: h.PlayerOId,
					h.StartedAt,
					h.EndedAt,
					Winner = h.Winner
				}).ToList();

				var moveDtos = moves.Select(m => new
				{
					m.Id,
					m.GameHistoryId,
					m.PlayerSymbol,
					m.Row,
					m.Col,
					m.MoveTime
				}).ToList();

				await Clients.Caller.SendAsync("ReceiveHistory", new
				{
					histories = historyDtos,
					moves = moveDtos
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error in RequestHistory: " + ex);
				await Clients.Caller.SendAsync("Error", "Lỗi khi tải lịch sử trận đấu: " + ex.Message);
			}
		}

		public async Task InviteFriendToRoom(string friendId, string inviterId)
		{
			var roomId = GenerateRoomCode();

			// ✅ TẠO ROOM + THÊM NGƯỜI MỜI NHƯ JOINROOM THÔNG THƯỜNG
			await JoinRoom(roomId, Context.User?.Identity?.Name ?? "Người mời");  // ← CHÌA KHÓA!

			// Gửi lời mời cho bạn
			await Clients.User(friendId).SendAsync("ReceiveGameInvite", new
			{
				RoomId = roomId,
				SenderId = inviterId,
				SenderName = Context.User?.Identity?.Name ?? "Người mời"
			});

			await Clients.Caller.SendAsync("RoomCreated", roomId);  // Cho người mời biết
		}

		// Hàm tạo mã phòng ngẫu nhiên
		private string GenerateRoomCode()
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			var random = new Random();
			return new string(Enumerable.Repeat(chars, 6)
				.Select(s => s[random.Next(s.Length)]).ToArray());
		}

		public async Task InvitePlayer(string roomId, string inviterName, string inviteeConnectionId)
		{
			// Notify the invitee about the successful invitation
			await Clients.Client(inviteeConnectionId).SendAsync("invitesuccess", roomId, inviterName);
		}

		public async Task AcceptInvite(string roomId)
		{
			var userId = Context.UserIdentifier;
			if (string.IsNullOrEmpty(userId))
			{
				await Clients.Caller.SendAsync("Error", "User not authenticated.");
				return;
			}

			if (!_rooms.TryGetValue(roomId, out var room))
			{
				await Clients.Caller.SendAsync("Error", "Room does not exist.");
				return;
			}

			// Add accepter to group so group broadcasts reach them and they are part of room
			await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

			// Notify group (host + any others)
			await Clients.Group(roomId).SendAsync("InviteAccepted", new
			{
				RoomId = roomId,
				UserId = userId,
				UserName = Context.User?.Identity?.Name ?? "Unknown User"
			});

			// Also send a direct success event to the caller (invitee) so client can redirect reliably
			await Clients.Caller.SendAsync("invitesuccess", roomId, room.HostName);

			// Optionally notify host (by user id) directly as well:
			if (!string.IsNullOrEmpty(room.HostId))
			{
				await Clients.User(room.HostId).SendAsync("InviteAccepted", roomId, userId, Context.User?.Identity?.Name);
			}
		}
	}
}