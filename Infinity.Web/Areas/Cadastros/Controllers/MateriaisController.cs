using Infinity.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.Web.Areas.Cadastros.Controllers
{
    [Area("Cadastros")]
    [Authorize]
    public class MateriaisController : Controller
    {
        private readonly AppDbContext _db;
        public MateriaisController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? busca, string? cor, string? status, string? fornecedor, string? tipoMaterial)
        {
            // Carrega opções únicas para os filtros
            ViewBag.TiposMaterial = await _db.Materiais
                .AsNoTracking()
                .Where(m => !string.IsNullOrEmpty(m.TipoMaterial))
                .Select(m => m.TipoMaterial)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            ViewBag.Fornecedores = await _db.Materiais
                .AsNoTracking()
                .Where(m => !string.IsNullOrEmpty(m.Fornecedor))
                .Select(m => m.Fornecedor)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();

            ViewBag.Cores = await _db.Materiais
                .AsNoTracking()
                .Where(m => !string.IsNullOrEmpty(m.Cor))
                .Select(m => m.Cor!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var query = _db.Materiais.AsNoTracking();

            // Filtro por Status (Ativo / Desativado)
            if (status == "ativos")
                query = query.Where(m => m.Ativo);
            else if (status == "inativos")
                query = query.Where(m => !m.Ativo);

            // Filtro por Tipo de Material
            if (!string.IsNullOrWhiteSpace(tipoMaterial))
                query = query.Where(m => m.TipoMaterial == tipoMaterial);

            // Filtro por Fornecedor
            if (!string.IsNullOrWhiteSpace(fornecedor))
                query = query.Where(m => m.Fornecedor == fornecedor);

            // Filtro por Cor
            if (!string.IsNullOrWhiteSpace(cor))
                query = query.Where(m => m.Cor == cor);

            // Busca por texto livre
            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();
                query = query.Where(m =>
                    m.Fornecedor.Contains(busca) ||
                    (m.Cor != null && m.Cor.Contains(busca)) ||
                    m.TipoMaterial.Contains(busca));
            }

            var lista = await query.OrderBy(m => m.TipoMaterial).ThenBy(m => m.Fornecedor).ToListAsync();

            ViewBag.Busca = busca;
            ViewBag.CorSelecionada = cor;
            ViewBag.StatusSelecionado = status;
            ViewBag.FornecedorSelecionado = fornecedor;
            ViewBag.TipoMaterialSelecionado = tipoMaterial;

            return View(lista);
        }

        [HttpGet]
        public IActionResult Criar() => View(new Material { Valor = 0 });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Material model)
        {
            if (!ModelState.IsValid) return View(model);
            model.Ativo = true;
            _db.Add(model);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Material cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var m = await _db.Materiais.FindAsync(id);
            if (m == null) return NotFound();
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Material model)
        {
            var mat = await _db.Materiais.FindAsync(model.Id);
            if (mat == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            mat.Fornecedor = model.Fornecedor;
            mat.Cor = model.Cor;
            mat.Valor = model.Valor;
            mat.PesoCompra = model.PesoCompra;
            mat.TipoMaterial = model.TipoMaterial;
            mat.Descricao = model.Descricao;
            mat.Ativo = model.Ativo;

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Material atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var m = await _db.Materiais.FindAsync(id);
            if (m != null)
            {
                _db.Materiais.Remove(m); // exclusão definitiva do banco de dados
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Material excluído permanentemente do banco de dados.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
