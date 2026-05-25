using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartQueue.Api.Models;
using SmartQueue.Api.DTOs;

namespace SmartQueue.Api.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;

        public AccountController(SignInManager<ApplicationUser> signInManager)
        {
            this.signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DebugMessage = "ModelState is invalid.";
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ViewBag.DebugMessage =
                    $"Login failed. Email: {model.Email}, Password length: {model.Password?.Length}, " +
                    $"LockedOut: {result.IsLockedOut}, NotAllowed: {result.IsNotAllowed}, Requires2FA: {result.RequiresTwoFactor}";

                return View(model);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Dashboard");
        }
    }
}