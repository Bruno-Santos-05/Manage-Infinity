using Infinity.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.Web.Areas.Cadastros.Controllers
{
    [Area("Cadastros")]
    [Authorize]
    public class GruposController : Controller
    {
        private readonly AppDbContext _db;
        public GruposController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var lista = await _db.Grupos.AsNoTracking().Where(g => g.Ativo).OrderBy(g => g.Nome).ToListAsync();
            return View(lista);
        }

        [HttpGet]
        public IActionResult Criar() => View(new Grupo());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Grupo model)
        {
            if (!ModelState.IsValid) return View(model);
            model.Ativo = true;
            _db.Add(model);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Grupo cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var g = await _db.Grupos.FindAsync(id);
            if (g == null) return NotFound();
            return View(g);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Grupo model)
        {
            var grp = await _db.Grupos.FindAsync(model.Id);
            if (grp == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            grp.Nome = model.Nome;
            grp.Descricao = model.Descricao;
            grp.Ativo = model.Ativo;

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Grupo atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var g = await _db.Grupos.FindAsync(id);
            if (g != null)
            {
                g.Ativo = false;
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Grupo excluído.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
