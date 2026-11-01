using Infinity.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.Web.Areas.Cadastros.Controllers
{
    [Area("Cadastros")]
    [Authorize]
    public class EmbalagensController : Controller
    {
        private readonly AppDbContext _db;
        public EmbalagensController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? busca)
        {
            var query = _db.Embalagens.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();
                query = query.Where(e => e.Nome.Contains(busca));
            }

            var lista = await query.OrderBy(e => e.Nome).ToListAsync();
            ViewBag.Busca = busca;
            return View(lista);
        }

        [HttpGet]
        public IActionResult Criar() => View(new Embalagem { ValorUnitario = 0 });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Embalagem model)
        {
            if (!ModelState.IsValid) return View(model);
            model.Ativo = true;
            _db.Embalagens.Add(model);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Embalagem cadastrada com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var e = await _db.Embalagens.FindAsync(id);
            if (e == null) return NotFound();
            return View(e);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Embalagem model)
        {
            var emb = await _db.Embalagens.FindAsync(model.Id);
            if (emb == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            emb.Nome = model.Nome;
            emb.ValorUnitario = model.ValorUnitario;
            emb.Ativo = model.Ativo;

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Embalagem atualizada com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var e = await _db.Embalagens.FindAsync(id);
            if (e != null)
            {
                _db.Embalagens.Remove(e);
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Embalagem excluída permanentemente do banco de dados.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
