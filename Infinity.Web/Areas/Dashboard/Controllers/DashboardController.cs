using Infinity.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infinity.Web.Services;

namespace Infinity.Web.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;
        public DashboardController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var model = new DashboardVM();

            // Projetos ativos com dados completos
            var projetos = await _db.Projetos
                .AsNoTracking()
                .Include(p => p.Grupo)
                .Include(p => p.NivelDificuldade)
                .Include(p => p.ProjetoMateriais)!.ThenInclude(pm => pm.Material)
                .Include(p => p.ProjetoEmbalagens)!.ThenInclude(pe => pe.Embalagem)
                .Where(p => p.Ativo)
                .ToListAsync();

            // ---- Resumo geral ----
            model.TotalProjetos = projetos.Count;
            model.TotalPagos = projetos.Count(p => p.Pago);
            model.TotalReceber = projetos.Count(p => !p.Pago);

            // ---- Financeiro consolidado (estimado vs real) ----
            var fins = projetos.Select(FinanceCalc.Calcular).ToList();
            model.TotalLucroEstimado = fins.Sum(f => f.LucroEstimado);
            model.TotalLucroReal = fins.Sum(f => f.LucroReal);
            model.TotalCustoReal = fins.Sum(f => f.CustoTotalReal);

            // ---- Projetos mais lucrativos (por lucro ESTIMADO; mostramos também o real) ----
            var comLucro = projetos.Select(p => new ProjetoRanking
            {
                Projeto = p,
                GrupoNome = p.Grupo?.Nome ?? "Sem grupo",
                CustoTotal = FinanceCalc.Calcular(p).CustoTotalReal,
                Financeiro = FinanceCalc.Calcular(p)
            }).ToList();

            model.MaisLucrativos = comLucro
                .OrderByDescending(x => x.Financeiro.LucroReal)
                .Take(10)
                .ToList();

            // ---- Projetos com maior diferença entre gasto real e estimado ----
            model.MaiorDistanciaCusto = projetos
                .Select(p => new DistanciaCustoVM
                {
                    Projeto = p,
                    GrupoNome = p.Grupo?.Nome ?? "Sem grupo",
                    Financeiro = FinanceCalc.Calcular(p)
                })
                .OrderByDescending(x => Math.Abs(x.Financeiro.DiffCusto))
                .Take(10)
                .ToList();

            // ---- Projetos com maior diferença entre lucro REAL e lucro ESTIMADO ----
            // (só faz sentido se houver dados reais; caso contrário o lucro real é 0)
            model.MaiorDistanciaLucro = projetos
                .Select(p => new DistanciaLucroVM
                {
                    Projeto = p,
                    GrupoNome = p.Grupo?.Nome ?? "Sem grupo",
                    Financeiro = FinanceCalc.Calcular(p)
                })
                .Where(x => x.Financeiro.TemDadosReais)
                .OrderByDescending(x => Math.Abs(x.Financeiro.DiffLucro))
                .Take(10)
                .ToList();

            // ---- TOP 5 produtos (materiais) com maior LUCRO REAL ----
            // Atribui o lucro real dos projetos aos materiais que eles utilizaram.
            model.MateriaisMaisLucrativos = projetos
                .Select(p => new { p, fin = FinanceCalc.Calcular(p) })
                .SelectMany(x => x.p.ProjetoMateriais!
                    .Where(pm => pm.Material != null)
                    .Select(pm => new { x.p, x.fin, pm }))
                .Where(x => x.fin.TemDadosReais)
                .GroupBy(x => x.pm.Material!.TipoMaterial)
                .Select(g => new MaterialLucroRanking
                {
                    NomeMaterial = g.Key,
                    TotalProjetos = g.Select(x => x.p.Id).Distinct().Count(),
                    LucroReal = g.Sum(x => x.fin.LucroReal),
                    QuantidadeReal = g.Sum(x => x.p.MaterialReal)
                })
                .OrderByDescending(x => x.LucroReal)
                .Take(5)
                .ToList();

            // ---- Mais lucrativos separados por grupo (compara pelo lucro REAL) ----
            model.PorGrupo = comLucro
                .Where(x => !string.IsNullOrEmpty(x.GrupoNome))
                .GroupBy(x => x.GrupoNome)
                .Select(g => new GrupoResumo
                {
                    NomeGrupo = g.Key,
                    TotalProjetos = g.Count(),
                    TotalLucroEstimado = g.Sum(x => x.Financeiro.LucroEstimado),
                    TotalLucro = g.Sum(x => x.Financeiro.LucroReal),
                    TotalCusto = g.Sum(x => x.Financeiro.CustoTotalReal),
                    TotalVenda = g.Sum(x => x.Financeiro.ValorVenda),
                    MaisLucrativo = g.OrderByDescending(x => x.Financeiro.LucroReal)
                                     .Select(x => x.Projeto.Nome + " (" + x.Financeiro.LucroReal.ToString("C") + ")")
                                     .FirstOrDefault() ?? "-"
                })
                .OrderByDescending(g => g.TotalLucro)
                .ToList();

            // ---- Projetos que mais vendem em quantidade de material ----
            model.MaisVendemMaterial = projetos
                .Select(p => new
                {
                    Projeto = p,
                    Qtd = p.ProjetoMateriais!.Sum(pm => pm.Quantidade),
                    QtdReal = p.MaterialReal
                })
                .Select(x => new MaterialRanking
                {
                    ProjetoNome = x.Projeto.Nome,
                    QuantidadeEstimada = x.Qtd,
                    QuantidadeReal = x.QtdReal,
                    GrupoNome = x.Projeto.Grupo?.Nome ?? "-"
                })
                .OrderByDescending(x => x.QuantidadeReal + x.QuantidadeEstimada)
                .Take(10)
                .ToList();

            // ---- Ranking de materiais mais usados (por quantidade total) ----
            var materiaisMap = new Dictionary<string, decimal>();
            foreach (var p in projetos)
            {
                if (p.ProjetoMateriais == null) continue;
                foreach (var pm in p.ProjetoMateriais)
                {
                    var nome = pm.Material?.TipoMaterial ?? "Material";
                    if (!materiaisMap.ContainsKey(nome)) materiaisMap[nome] = 0;
                    materiaisMap[nome] += pm.Quantidade;
                }
            }
            model.MateriaisMaisUsados = materiaisMap
                .Select(kv => new MaterialRanking { ProjetoNome = kv.Key, QuantidadeEstimada = kv.Value })
                .OrderByDescending(x => x.QuantidadeEstimada)
                .Take(10)
                .ToList();

            // ---- Relatório: "Nos conheceu por onde?" (distribuição de clientes por origem) ----
            var clientes = await _db.Clientes
                .AsNoTracking()
                .Include(c => c.Origem)
                .Where(c => c.Ativo)
                .ToListAsync();

            model.Origens = clientes
                .Where(c => c.Origem != null)
                .GroupBy(c => c.Origem!.Nome)
                .Select(g => new OrigemResumoDashboard
                {
                    NomeOrigem = g.Key,
                    Quantidade = g.Count(),
                    Nomes = g.Select(c => c.NomeCompleto).Take(5).ToList()
                })
                .OrderByDescending(o => o.Quantidade)
                .ToList();

            return View(model);
        }

        public async Task<IActionResult> ExportarOrigensExcel()
        {
            var itens = await _db.Clientes
                .AsNoTracking()
                .Include(c => c.Origem)
                .Where(c => c.Ativo && c.Origem != null)
                .ToListAsync();

            var agrupado = itens
                .GroupBy(c => c.Origem!.Nome)
                .Select(g => new OrigemResumoVM
                {
                    NomeOrigem = g.Key,
                    Quantidade = g.Count(),
                    Clientes = g.Select(c => c.NomeCompleto + (string.IsNullOrEmpty(c.NumeroCelular) ? "" : " (" + c.NumeroCelular + ")")).ToList()
                })
                .OrderByDescending(o => o.Quantidade)
                .ToList();

            var bytes = RelatorioService.OrigensExcel(agrupado, "Relatório Nos Conheceu Por Onde?", DateTime.Now);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "NosConheceuPorOnde.xlsx");
        }
    }

    public class DashboardVM
    {
        public int TotalProjetos { get; set; }
        public int TotalPagos { get; set; }
        public int TotalReceber { get; set; }
        public decimal TotalLucroEstimado { get; set; }
        public decimal TotalLucroReal { get; set; }
        public decimal TotalCustoReal { get; set; }
        public List<ProjetoRanking> MaisLucrativos { get; set; } = new();
        public List<DistanciaCustoVM> MaiorDistanciaCusto { get; set; } = new();
        public List<DistanciaLucroVM> MaiorDistanciaLucro { get; set; } = new();
        public List<MaterialLucroRanking> MateriaisMaisLucrativos { get; set; } = new();
        public List<GrupoResumo> PorGrupo { get; set; } = new();
        public List<MaterialRanking> MaisVendemMaterial { get; set; } = new();
        public List<MaterialRanking> MateriaisMaisUsados { get; set; } = new();
        public List<OrigemResumoDashboard> Origens { get; set; } = new();
    }

    public class OrigemResumoDashboard
    {
        public string NomeOrigem { get; set; } = "-";
        public int Quantidade { get; set; }
        public List<string> Nomes { get; set; } = new();
    }

    public class ProjetoRanking
    {
        public Projeto Projeto { get; set; } = new();
        public string GrupoNome { get; set; } = "-";
        public decimal CustoTotal { get; set; }
        public ProjetoFinanceiro Financeiro { get; set; } = new();
    }

    public class DistanciaCustoVM
    {
        public Projeto Projeto { get; set; } = new();
        public string GrupoNome { get; set; } = "-";
        public ProjetoFinanceiro Financeiro { get; set; } = new();
    }

    public class GrupoResumo
    {
        public string NomeGrupo { get; set; } = "";
        public int TotalProjetos { get; set; }
        public decimal TotalCusto { get; set; }
        public decimal TotalVenda { get; set; }
        public decimal TotalLucroEstimado { get; set; }
        public decimal TotalLucro { get; set; }
        public string MaisLucrativo { get; set; } = "-";
    }

    public class MaterialRanking
    {
        public string ProjetoNome { get; set; } = "";
        public string GrupoNome { get; set; } = "";
        public decimal QuantidadeEstimada { get; set; }
        public decimal QuantidadeReal { get; set; }
    }

    public class DistanciaLucroVM
    {
        public Projeto Projeto { get; set; } = new();
        public string GrupoNome { get; set; } = "-";
        public ProjetoFinanceiro Financeiro { get; set; } = new();
    }

    public class MaterialLucroRanking
    {
        public string NomeMaterial { get; set; } = "";
        public int TotalProjetos { get; set; }
        public decimal LucroReal { get; set; }
        public decimal QuantidadeReal { get; set; }
    }
}
