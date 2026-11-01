using System.Security.Claims;
using Infinity.Web.Helpers;
using Infinity.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Infinity.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LoginController : Controller
    {
        private readonly AppDbContext _db;
        public LoginController(AppDbContext db) => _db = db;

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Index(string? retorno)
        {
            if (User.Identity!.IsAuthenticated)
                return RedirectToAction("Index", "Home", new { area = "" });

            ViewBag.Retorno = retorno;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Index(LoginViewModel model, string? retorno)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Retorno = retorno;
                return View(model);
            }

            var usuario = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.Login == model.Login && u.Ativo);

            if (usuario == null || usuario.Senha != Cripto.HashSha256(model.Senha))
            {
                ModelState.AddModelError(string.Empty, "Login ou senha inválidos.");
                ViewBag.Retorno = retorno;
                return View(model);
            }

            // Monta claims do usuário
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Login),
                new Claim("Nome", usuario.Nome)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            if (!string.IsNullOrEmpty(retorno) && Url.IsLocalUrl(retorno))
                return Redirect(retorno);

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Sair()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AcessoNegado() => View();
    }

    public class LoginViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Informe o login.")]
        public string Login { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Informe a senha.")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Senha { get; set; } = string.Empty;
    }
}
