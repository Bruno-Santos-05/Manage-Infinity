using Infinity.Web.Helpers;
using Infinity.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _db;
        public UsuariosController(AppDbContext db) => _db = db;

        // Listar usuários
        public async Task<IActionResult> Index()
        {
            var lista = await _db.Usuarios.OrderBy(u => u.Nome).ToListAsync();
            return View(lista);
        }

        [HttpGet]
        public IActionResult Criar() => View(new Usuario());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Usuario model)
        {
            if (_db.Usuarios.Any(u => u.Login == model.Login))
                ModelState.AddModelError("Login", "Este login já está em uso.");

            if (!ModelState.IsValid)
                return View(model);

            model.Senha = Cripto.HashSha256(model.Senha);
            model.Ativo = true;

            _db.Add(model);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Usuário cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var u = await _db.Usuarios.FindAsync(id);
            if (u == null) return NotFound();
            return View(u);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Usuario model)
        {
            var usuario = await _db.Usuarios.FindAsync(model.Id);
            if (usuario == null) return NotFound();

            if (_db.Usuarios.Any(u => u.Login == model.Login && u.Id != model.Id))
                ModelState.AddModelError("Login", "Este login já está em uso.");

            if (string.IsNullOrWhiteSpace(model.Senha))
                ModelState.Remove(nameof(model.Senha));
            else if (model.Senha != "********")
                ModelState.Remove(nameof(model.Senha));

            if (!ModelState.IsValid)
                return View(model);

            usuario.Nome = model.Nome;
            usuario.Login = model.Login;
            usuario.Ativo = model.Ativo;

            // Só altera a senha se uma nova foi informada
            if (!string.IsNullOrWhiteSpace(model.Senha) && model.Senha != "********")
                usuario.Senha = Cripto.HashSha256(model.Senha);

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Usuário atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var u = await _db.Usuarios.FindAsync(id);
            if (u != null)
            {
                _db.Usuarios.Remove(u);
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Usuário excluído.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
