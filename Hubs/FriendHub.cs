using Microsoft.AspNetCore.SignalR;

public class FriendHub : Hub
{
	// Khi user kết nối thì join vào group riêng của họ
	public override async Task OnConnectedAsync()
	{
		var userId = Context.UserIdentifier;
		if (!string.IsNullOrEmpty(userId))
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
		}

		await base.OnConnectedAsync();
	}
}

