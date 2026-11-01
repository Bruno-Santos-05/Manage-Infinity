using Infinity.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.Web.Areas.Cadastros.Controllers
{
    [Area("Cadastros")]
    [Authorize]
    public class ConfiguracoesController : Controller
    {
        private readonly AppDbContext _db;
        public ConfiguracoesController(AppDbContext db) => _db = db;

        public class ConfigViewModel
        {
            public decimal LucroDesejadoPadrao { get; set; }
            public decimal ValorHoraMaquinaPadrao { get; set; }
            public string MetodoPagamentoPadrao { get; set; } = "PIX";
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cfg = await _db.Configuracoes.ToDictionaryAsync(c => c.Chave, c => c.Valor);
            var model = new ConfigViewModel
            {
                LucroDesejadoPadrao = DecimalOpt(cfg, "LucroDesejadoPadrao", 30),
                ValorHoraMaquinaPadrao = DecimalOpt(cfg, "ValorHoraMaquinaPadrao", 50),
                MetodoPagamentoPadrao = StrOpt(cfg, "MetodoPagamentoPadrao", "PIX")
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ConfigViewModel model)
        {
            await SetValor("LucroDesejadoPadrao", model.LucroDesejadoPadrao.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await SetValor("ValorHoraMaquinaPadrao", model.ValorHoraMaquinaPadrao.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await SetValor("MetodoPagamentoPadrao", model.MetodoPagamentoPadrao);

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Configurações salvas com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        private async Task SetValor(string chave, string? valor)
        {
            var cfg = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == chave);
            if (cfg == null)
            {
                _db.Configuracoes.Add(new Configuracao { Chave = chave, Valor = valor, Descricao = chave });
            }
            else
            {
                cfg.Valor = valor;
            }
        }

        private static decimal DecimalOpt(Dictionary<string, string?> cfg, string chave, decimal padrao)
            => decimal.TryParse(StrOpt(cfg, chave, padrao.ToString()), System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : padrao;

        private static string StrOpt(Dictionary<string, string?> cfg, string chave, string padrao)
            => cfg.TryGetValue(chave, out var v) && !string.IsNullOrWhiteSpace(v) ? v! : padrao;
    }
}
