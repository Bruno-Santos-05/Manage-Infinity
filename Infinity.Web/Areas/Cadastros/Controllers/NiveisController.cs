using Infinity.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.Web.Areas.Cadastros.Controllers
{
    [Area("Cadastros")]
    [Authorize]
    public class NiveisController : Controller
    {
        private readonly AppDbContext _db;
        public NiveisController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var lista = await _db.NiveisDificuldade.AsNoTracking().Where(n => n.Ativo).OrderBy(n => n.Nome).ToListAsync();
            return View(lista);
        }

        [HttpGet]
        public IActionResult Criar() => View(new NivelDificuldade());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(NivelDificuldade model)
        {
            if (!ModelState.IsValid) return View(model);
            model.Ativo = true;
            _db.Add(model);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Nível cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var n = await _db.NiveisDificuldade.FindAsync(id);
            if (n == null) return NotFound();
            return View(n);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(NivelDificuldade model)
        {
            var niv = await _db.NiveisDificuldade.FindAsync(model.Id);
            if (niv == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            niv.Nome = model.Nome;
            niv.Descricao = model.Descricao;
            niv.LucroPercentual = model.LucroPercentual;
            niv.Ativo = model.Ativo;

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Nível atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var n = await _db.NiveisDificuldade.FindAsync(id);
            if (n != null)
            {
                n.Ativo = false;
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Nível excluído.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
