using Infinity.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.Web.Areas.Cadastros.Controllers
{
    [Area("Cadastros")]
    [Authorize]
    public class OrigensController : Controller
    {
        private readonly AppDbContext _db;
        public OrigensController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var lista = await _db.Origens.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Nome).ToListAsync();
            return View(lista);
        }

        [HttpGet]
        public IActionResult Criar() => View(new Origem());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Origem model)
        {
            if (!ModelState.IsValid) return View(model);
            model.Ativo = true;
            _db.Add(model);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Origem cadastrada com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var o = await _db.Origens.FindAsync(id);
            if (o == null) return NotFound();
            return View(o);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Origem model)
        {
            var origem = await _db.Origens.FindAsync(model.Id);
            if (origem == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            origem.Nome = model.Nome;
            origem.Descricao = model.Descricao;
            origem.Ativo = model.Ativo;

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Origem atualizada com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var o = await _db.Origens.FindAsync(id);
            if (o != null)
            {
                o.Ativo = false;
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Origem excluída.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
