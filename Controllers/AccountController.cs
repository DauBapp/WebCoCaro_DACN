using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Web_chơi_cờ_Caro.Data;
using Web_chơi_cờ_Caro.Hubs;
using Web_chơi_cờ_Caro.Models;

namespace Web_chơi_cờ_Caro.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public UsersController(ApplicationDbContext db)
        {
            _db = db;
        }

		[HttpGet("Search")]
		public async Task<IActionResult> Search(string term)
		{
			if (string.IsNullOrWhiteSpace(term))
				return Ok(new List<object>());

			var currentUserName = User.Identity?.Name ?? "";

			var users = await _db.Users
				.Where(u => (u.UserName != null && u.UserName.Contains(term)) || (u.Id != null && u.Id.Contains(term)))
				.Where(u => u.UserName != currentUserName)
				.Select(u => new
				{
					id = u.Id,
					name = u.UserName,
					status = u.Status ?? "offline",
					//avatarUrl = "/images/default-avatar.png"
                    avatarUrl = string.IsNullOrEmpty(u.AvatarUrl) 
                        ? "/images/default-avatar.png" 
                        : u.AvatarUrl
				})
				.Take(10)
				.ToListAsync();

			return Ok(users);
		}
	}


	public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly ApplicationDbContext _db;

		public AccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IHubContext<PresenceHub> presenceHub,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _presenceHub = presenceHub;
            _db = db;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home"); // đăng ký thành công quay về trang chính
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    // Cập nhật thời gian đăng nhập cuối
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        user.LastLoginTime = DateTime.Now;
                        await _userManager.UpdateAsync(user);
                    }
                    
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Sai email hoặc mật khẩu.");
            }
            return View();
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? user.Email ?? "" : user.DisplayName,
                CreatedAt = user.CreatedAt,
                LastLoginTime = user.LastLoginTime,
                AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) ? "/images/default-avatar.png" : user.AvatarUrl
            };

            return View(model);
        }

        // POST: /Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Update email if changed
            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }
            }

            // Update display name (nickname) - keep login info unchanged
            if (user.DisplayName != model.DisplayName)
            {
                user.DisplayName = model.DisplayName;
            }

            // Handle avatar upload
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                try
                {
                    var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
                    if (!Directory.Exists(uploadsRoot))
                    {
                        Directory.CreateDirectory(uploadsRoot);
                    }

                    var ext = Path.GetExtension(model.AvatarFile.FileName);
                    if (string.IsNullOrEmpty(ext)) ext = ".png";
                    var fileName = $"{user.Id}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext}";
                    var filePath = Path.Combine(uploadsRoot, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.AvatarFile.CopyToAsync(stream);
                    }

                    user.AvatarUrl = $"/images/avatars/{fileName}";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Tải ảnh đại diện thất bại: " + ex.Message);
                    return View(model);
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                // Refresh authentication cookie so User.Identity.Name reflects the latest username
                await _signInManager.RefreshSignInAsync(user);
                
                // ✅ Gửi thông báo cập nhật avatar cho bạn bè đang online
                if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                {
                    try
                    {
                        var friends = await _db.Friends
                            .Where(f => f.UserId == user.Id)
                            .Select(f => f.FriendId)
                            .ToListAsync();

                        var updatedAvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) 
                            ? "/images/default-avatar.png" 
                            : user.AvatarUrl;

                        // Gửi thông báo cho tất cả bạn bè
                        foreach (var friendId in friends)
                        {
                            await _presenceHub.Clients.Group($"user_{friendId}").SendAsync("AvatarUpdated", new
                            {
                                id = user.Id,
                                name = user.UserName,
                                avatarUrl = updatedAvatarUrl
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nhưng không làm gián đoạn quá trình cập nhật
                        Console.WriteLine($"Lỗi khi gửi thông báo cập nhật avatar: {ex.Message}");
                    }
                }
                
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // GET: /Account/ChangePassword
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Kiểm tra mật khẩu hiện tại
            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            // Kiểm tra mật khẩu mới không trùng với mật khẩu cũ
            if (model.CurrentPassword == model.NewPassword)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải khác với mật khẩu hiện tại.");
                return View(model);
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    // Chuyển đổi thông báo lỗi sang tiếng Việt
                    var errorMessage = error.Description;
                    if (error.Code == "PasswordTooShort")
                    {
                        errorMessage = "Mật khẩu quá ngắn. Mật khẩu phải có ít nhất 6 ký tự.";
                    }
                    else if (error.Code == "PasswordRequiresDigit")
                    {
                        errorMessage = "Mật khẩu phải chứa ít nhất một chữ số (0-9).";
                    }
                    else if (error.Code == "PasswordRequiresLower")
                    {
                        errorMessage = "Mật khẩu phải chứa ít nhất một chữ cái thường (a-z).";
                    }
                    else if (error.Code == "PasswordRequiresUpper")
                    {
                        errorMessage = "Mật khẩu phải chứa ít nhất một chữ cái hoa (A-Z).";
                    }
                    else if (error.Code == "PasswordRequiresNonAlphanumeric")
                    {
                        errorMessage = "Mật khẩu phải chứa ít nhất một ký tự đặc biệt (!@#$%^&*...).";
                    }
                    else if (error.Code == "PasswordMismatch")
                    {
                        errorMessage = "Mật khẩu hiện tại không đúng.";
                    }
                    
                    ModelState.AddModelError("", errorMessage);
                }
                return View(model);
            }

            // Làm mới phiên đăng nhập sau khi đổi mật khẩu thành công
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("ChangePassword");
        }

        // GET: /Account/GameHistory
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GameHistory(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            const int maxGamesToKeep = 25; // Giữ tối đa 25 trận gần nhất
            const int pageSize = 10; // 10 trận mỗi trang

            // Xóa các trận cũ hơn 25 trận gần nhất
            var allGameIds = await _db.GameHistories
                .OrderByDescending(g => g.StartedAt)
                .Select(g => g.Id)
                .Take(maxGamesToKeep)
                .ToListAsync();

            if (allGameIds.Any())
            {
                // Xóa các trận không nằm trong top 25
                var gamesToDelete = await _db.GameHistories
                    .Where(g => !allGameIds.Contains(g.Id))
                    .ToListAsync();

                if (gamesToDelete.Any())
                {
                    // Xóa các MoveRecords liên quan trước
                    var gameIdsToDelete = gamesToDelete.Select(g => g.Id).ToList();
                    var movesToDelete = await _db.MoveRecords
                        .Where(m => gameIdsToDelete.Contains(m.GameHistoryId))
                        .ToListAsync();
                    _db.MoveRecords.RemoveRange(movesToDelete);

                    // Sau đó xóa GameHistories
                    _db.GameHistories.RemoveRange(gamesToDelete);
                    await _db.SaveChangesAsync();
                }
            }

            // Lấy 25 trận gần nhất (sắp xếp theo thời gian mới nhất)
            var totalGames = await _db.GameHistories.CountAsync();
            var actualTotalGames = Math.Min(totalGames, maxGamesToKeep);
            var totalPages = (int)Math.Ceiling(actualTotalGames / (double)pageSize);

            // Đảm bảo page hợp lệ
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var gameHistories = await _db.GameHistories
                .Include(g => g.Moves)
                .OrderByDescending(g => g.StartedAt)
                .Take(maxGamesToKeep) // Chỉ lấy 25 trận gần nhất
                .Skip((page - 1) * pageSize) // Pagination
                .Take(pageSize) // 10 trận mỗi trang
                .ToListAsync();

            var viewModel = new GameHistoryViewModel
            {
                GameHistories = gameHistories.Select(g => new GameHistoryItemViewModel
                {
                    Id = g.Id,
                    RoomId = g.RoomId,
                    PlayerXId = g.PlayerXId,
                    PlayerXName = g.PlayerXId, // Tạm thời hiển thị ConnectionId, có thể cải thiện sau
                    PlayerOId = g.PlayerOId,
                    PlayerOName = g.PlayerOId, // Tạm thời hiển thị ConnectionId, có thể cải thiện sau
                    StartedAt = g.StartedAt,
                    EndedAt = g.EndedAt,
                    Winner = g.Winner,
                    MoveCount = g.Moves?.Count ?? 0
                }).ToList(),
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalGames = actualTotalGames
            };

            return View(viewModel);
        }

        // GET: /Account/GetGameMoves
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetGameMoves(int gameId)
        {
            var moves = await _db.MoveRecords
                .Where(m => m.GameHistoryId == gameId)
                .OrderBy(m => m.MoveTime)
                .Select(m => new
                {
                    m.Id,
                    m.GameHistoryId,
                    m.PlayerSymbol,
                    m.Row,
                    m.Col,
                    m.MoveTime
                })
                .ToListAsync();

            return Json(moves);
        }
    }

    public class ProfileViewModel
    {
        public string Email { get; set; } = "";
        public string UserName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public string? AvatarUrl { get; set; }
        [System.ComponentModel.DataAnnotations.Display(Name = "Ảnh đại diện")]
        public IFormFile? AvatarFile { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class GameHistoryViewModel
    {
        public List<GameHistoryItemViewModel> GameHistories { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalGames { get; set; } = 0;
    }

    public class GameHistoryItemViewModel
    {
        public int Id { get; set; }
        public string RoomId { get; set; } = "";
        public string PlayerXId { get; set; } = "";
        public string PlayerXName { get; set; } = "";
        public string PlayerOId { get; set; } = "";
        public string PlayerOName { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string? Winner { get; set; }
        public int MoveCount { get; set; }
    }
} 