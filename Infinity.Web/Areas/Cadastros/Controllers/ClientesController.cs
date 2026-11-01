using Infinity.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Infinity.Web.Areas.Cadastros.Controllers
{
    [Area("Cadastros")]
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly AppDbContext _db;
        public ClientesController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? busca)
        {
            var query = _db.Clientes.AsNoTracking().Include(c => c.Origem).Where(c => c.Ativo);
            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();
                query = query.Where(c =>
                    c.NomeCompleto.Contains(busca) ||
                    (c.NumeroCelular != null && c.NumeroCelular.Contains(busca)) ||
                    (c.Origem != null && c.Origem.Nome.Contains(busca)));
            }

            var lista = await query.OrderBy(c => c.NomeCompleto).ToListAsync();
            ViewBag.Busca = busca;
            return View(lista);
        }

        [HttpGet]
        public async Task<IActionResult> Criar()
        {
            await CarregarOrigens();
            return View(new Cliente());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Cliente model)
        {
            if (!ModelState.IsValid)
            {
                await CarregarOrigens();
                return View(model);
            }
            model.Ativo = true;
            _db.Add(model);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Cliente cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var c = await _db.Clientes.FindAsync(id);
            if (c == null) return NotFound();
            await CarregarOrigens(c.OrigemId?.ToString());
            return View(c);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Cliente model)
        {
            var cli = await _db.Clientes.FindAsync(model.Id);
            if (cli == null) return NotFound();
            if (!ModelState.IsValid)
            {
                await CarregarOrigens(model.OrigemId?.ToString());
                return View(model);
            }

            cli.NomeCompleto = model.NomeCompleto;
            cli.NumeroCelular = model.NumeroCelular;
            cli.OrigemId = model.OrigemId;
            cli.Obs = model.Obs;
            cli.Ativo = model.Ativo;

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Cliente atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var c = await _db.Clientes.FindAsync(id);
            if (c != null)
            {
                c.Ativo = false;
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Cliente excluído.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task CarregarOrigens(string? selecionada = null)
        {
            var origens = await _db.Origens.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Nome).ToListAsync();
            ViewBag.Origens = new SelectList(origens, "Id", "Nome", selecionada);
        }
    }
}
