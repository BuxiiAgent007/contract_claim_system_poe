using Microsoft.AspNetCore.Mvc;
using contract_claim_system.Models;
using contract_claim_system.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

// Alias to resolve namespace conflict
using AuthClaim = System.Security.Claims.Claim;

namespace contract_claim_system.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // GET: /Account/Login
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                try
                {
                    var user = _authService.Authenticate(model.Email, model.Password);

                    if (user != null)
                    {
                        // Create claims identity - using alias to avoid namespace conflict
                        var claims = new List<AuthClaim>
                        {
                            new AuthClaim(ClaimTypes.NameIdentifier, user.userID.ToString()),
                            new AuthClaim(ClaimTypes.Name, $"{user.full_names} {user.surname}"),
                            new AuthClaim(ClaimTypes.Email, user.email),
                            new AuthClaim(ClaimTypes.Role, user.role),
                            new AuthClaim("FullName", $"{user.full_names} {user.surname}")
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        _logger.LogInformation($"User {user.email} logged in.");

                        // Redirect based on role
                        return RedirectToRoleDashboard(user.role, returnUrl);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        _logger.LogWarning($"Failed login attempt for email: {model.Email}");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "An error occurred during login.");
                    _logger.LogError(ex, $"Error during login for email: {model.Email}");
                }
            }

            return View(model);
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists
                    if (_authService.EmailExists(model.Email))
                    {
                        ModelState.AddModelError("Email", "Email already registered.");
                        return View(model);
                    }

                    var user = new User
                    {
                        full_names = model.FullNames,
                        surname = model.Surname,
                        email = model.Email,
                        role = model.Role,
                        gender = model.Gender,
                        password = model.Password, // In production, hash this
                        date = DateTime.Now
                    };

                    if (_authService.Register(user))
                    {
                        _logger.LogInformation($"User {model.Email} registered successfully.");

                        TempData["Success"] = "Registration successful! You can now login.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "An error occurred during registration.");
                    _logger.LogError(ex, $"Error during registration for email: {model.Email}");
                }
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out.");
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Account/Profile
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login");
            }

            var user = _authService.GetUserById(userId);

            if (user == null)
            {
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login");
            }

            return View(user);
        }

        private IActionResult RedirectToRoleDashboard(string role, string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Coordinator" => RedirectToAction("CoordinatorDashboard", "Review"),
                "Manager" => RedirectToAction("ManagerDashboard", "Review"),
                "HR" => RedirectToAction("Dashboard", "HR"),
                _ => RedirectToAction("Index", "Home") // Default for Lecturers
            };
        }
    }
}