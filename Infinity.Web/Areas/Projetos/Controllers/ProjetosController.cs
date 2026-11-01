using Infinity.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Infinity.Web.Helpers;
using Infinity.Web.Services;

namespace Infinity.Web.Areas.Projetos.Controllers
{
    [Area("Projetos")]
    [Authorize]
    public class ProjetosController : Controller
    {
        private readonly AppDbContext _db;
        public ProjetosController(AppDbContext db) => _db = db;

        #region Listagem

        public async Task<IActionResult> Index(string? busca, string? pago, int? clienteId, int? grupoId)
        {
            ViewBag.Clientes = await _db.Clientes
                .AsNoTracking()
                .Where(c => c.Ativo)
                .OrderBy(c => c.NomeCompleto)
                .ToListAsync();

            ViewBag.Grupos = await _db.Grupos
                .AsNoTracking()
                .Where(g => g.Ativo)
                .OrderBy(g => g.Nome)
                .ToListAsync();

            var query = _db.Projetos
                .AsNoTracking()
                .Include(p => p.Grupo)
                .Include(p => p.NivelDificuldade)
                .Include(p => p.ProjetoClientes)!.ThenInclude(pc => pc.Cliente)
                .Include(p => p.ProjetoMateriais)!.ThenInclude(pm => pm.Material)
                .Include(p => p.ProjetoEmbalagens)!.ThenInclude(pe => pe.Embalagem)
                .Where(p => p.Ativo);

            // Filtro por Pago / Não Pago
            if (pago == "sim")
                query = query.Where(p => p.Pago);
            else if (pago == "nao")
                query = query.Where(p => !p.Pago);

            // Filtro por Cliente
            if (clienteId.HasValue)
                query = query.Where(p => p.ProjetoClientes!.Any(pc => pc.ClienteId == clienteId.Value));

            // Filtro por Grupo de Projetos
            if (grupoId.HasValue)
            {
                if (grupoId.Value == -1)
                    query = query.Where(p => p.GrupoId == null);
                else
                    query = query.Where(p => p.GrupoId == grupoId.Value);
            }

            // Busca por texto livre
            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();
                query = query.Where(p =>
                    p.Nome.Contains(busca) ||
                    (p.Grupo != null && p.Grupo.Nome.Contains(busca)) ||
                    p.ProjetoClientes!.Any(pc => pc.Cliente != null && pc.Cliente.NomeCompleto.Contains(busca)));
            }

            var projetos = await query.OrderByDescending(p => p.DataCriacao).ToListAsync();

            var lista = new List<ProjetoListaVM>();
            foreach (var p in projetos)
                lista.Add(MontarVM(p));

            ViewBag.Busca = busca;
            ViewBag.PagoSelecionado = pago;
            ViewBag.ClienteIdSelecionado = clienteId;
            ViewBag.GrupoIdSelecionado = grupoId;
            return View(lista);
        }

        public async Task<IActionResult> Detalhes(int id)
        {
            var p = await Carregar(id);
            if (p == null) return NotFound();
            return View(MontarVM(p));
        }

        public async Task<IActionResult> ExportarExcel()
        {
            var itens = await MontarFinanceiroVM();
            var bytes = RelatorioService.ProjetosExcel(itens, "Relatório Financeiro de Projetos", DateTime.Now);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Projetos.xlsx");
        }

        public async Task<IActionResult> ExportarPdf()
        {
            var itens = await MontarFinanceiroVM();
            var colunas = new[]
            {
                "Projeto","Grupo","Clientes","Mat Est","Mat Real","Custo Est","Custo Real","Venda","Lucro Est","Lucro Real"
            };
            var linhas = itens.Select(it => new[]
            {
                it.Projeto.Nome,
                it.GrupoNome,
                string.Join(", ", it.Clientes.Take(2)),
                it.Financeiro.MaterialEstimado.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                it.Financeiro.MaterialReal.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                it.Financeiro.CustoTotalEstimado.ToString("0.00"),
                it.Financeiro.CustoTotalReal.ToString("0.00"),
                it.Financeiro.ValorVenda.ToString("0.00"),
                it.Financeiro.LucroEstimado.ToString("0.00"),
                it.Financeiro.LucroReal.ToString("0.00")
            });
            var bytes = PdfReport.Gerar("Relatório Financeiro de Projetos",
                "Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), colunas, linhas);
            return File(bytes, "application/pdf", "Projetos.pdf");
        }

        private async Task<List<ProjetoFinanceiroVM>> MontarFinanceiroVM()
        {
            var projetos = await _db.Projetos
                .AsNoTracking()
                .Include(p => p.Grupo)
                .Include(p => p.NivelDificuldade)
                .Include(p => p.ProjetoClientes)!.ThenInclude(pc => pc.Cliente)
                .Include(p => p.ProjetoMateriais)!.ThenInclude(pm => pm.Material)
                .Include(p => p.ProjetoEmbalagens)!.ThenInclude(pe => pe.Embalagem)
                .Where(p => p.Ativo)
                .OrderByDescending(p => p.DataCriacao)
                .ToListAsync();

            return projetos.Select(p => new ProjetoFinanceiroVM
            {
                Projeto = p,
                Financeiro = FinanceCalc.Calcular(p),
                GrupoNome = p.Grupo?.Nome ?? "-",
                NivelNome = p.NivelDificuldade?.Nome ?? "-",
                Clientes = p.ProjetoClientes!.Select(pc => pc.Cliente!.NomeCompleto).ToList()
            }).ToList();
        }

        #endregion

        #region Criar

        [HttpGet]
        public async Task<IActionResult> Criar()
        {
            var cfg = await _db.Configuracoes.ToDictionaryAsync(c => c.Chave, c => c.Valor);

            var vm = new ProjetoViewModel
            {
                // Pré-preenche com as configurações globais (editáveis depois)
                LucroDesejadoPercentual = DecimalCfg(cfg, "LucroDesejadoPadrao", 30),
                ValorHoraMaquina = DecimalCfg(cfg, "ValorHoraMaquinaPadrao", 50)
            };
            await PreencherDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(ProjetoViewModel vm)
        {
            // Permite zero clientes/materiais no form? Obriga pelo menos 1.
            if (vm.ClienteIds == null || vm.ClienteIds.Count == 0)
                ModelState.AddModelError(nameof(vm.ClienteIds), "Selecione pelo menos um cliente.");
            else
            {
                var clientesValidos = await _db.Clientes.Where(c => c.Ativo && vm.ClienteIds.Contains(c.Id)).CountAsync();
                if (clientesValidos != vm.ClienteIds.Count)
                    ModelState.AddModelError(nameof(vm.ClienteIds), "Um dos clientes selecionados é inválido.");
            }

            var materiaisEscolhidos = vm.Materiais?
                .Where(m => m.MaterialId > 0 && m.Quantidade > 0)
                .ToList() ?? new List<ProjetoMaterialViewModel>();
            if (materiaisEscolhidos.Count == 0)
                ModelState.AddModelError(nameof(vm.Materiais), "Selecione pelo menos um material com quantidade.");

            var embalagensEscolhidas = vm.Embalagens?
                .Where(e => e.EmbalagemId > 0 && e.Quantidade > 0)
                .GroupBy(e => e.EmbalagemId)
                .Select(g => new ProjetoEmbalagemViewModel { EmbalagemId = g.Key, Quantidade = g.Sum(x => x.Quantidade) })
                .ToList() ?? new List<ProjetoEmbalagemViewModel>();

            if (!ModelState.IsValid)
            {
                await PreencherDropdowns(vm);
                return View(vm);
            }

            var projeto = new Projeto
            {
                Nome = vm.Nome,
                Descricao = vm.Descricao,
                GrupoId = vm.GrupoId,
                HorasEstimadas = vm.HorasEstimadas ?? 0,
                QtdMaterialEstimada = materiaisEscolhidos.Sum(m => m.Quantidade),
                Pintura = vm.Pintura,
                NivelDificuldadeId = vm.Pintura ? vm.NivelDificuldadeId : null,
                HorasReais = vm.HorasReais ?? 0,
                MaterialReal = vm.MaterialReal ?? 0,
                Pago = vm.Pago,
                MetodoPagamento = vm.MetodoPagamento,
                Entrega = vm.Entrega,
                ValorFrete = vm.Entrega ? (vm.ValorFrete ?? 0) : 0,
                LucroDesejadoPercentual = vm.LucroDesejadoPercentual,
                ValorHoraMaquina = vm.ValorHoraMaquina ?? 0,
                Ativo = true
            };

            var clienteIdsCriar = vm.ClienteIds!;
            projeto.ProjetoClientes = clienteIdsCriar
                .Select(cid => new ProjetoCliente { ClienteId = cid }).ToList();

            projeto.ProjetoMateriais = materiaisEscolhidos
                .Select(m => new ProjetoMaterial { MaterialId = m.MaterialId, Quantidade = m.Quantidade }).ToList();

            projeto.ProjetoEmbalagens = embalagensEscolhidas
                .Select(e => new ProjetoEmbalagem { EmbalagemId = e.EmbalagemId, Quantidade = e.Quantidade }).ToList();

            _db.Projetos.Add(projeto);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Projeto criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Editar

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var p = await Carregar(id);
            if (p == null) return NotFound();

            var vm = new ProjetoViewModel
            {
                Id = p.Id,
                Nome = p.Nome,
                Descricao = p.Descricao,
                GrupoId = p.GrupoId,
                HorasEstimadas = p.HorasEstimadas,
                QtdMaterialEstimada = p.QtdMaterialEstimada,
                Pintura = p.Pintura,
                NivelDificuldadeId = p.NivelDificuldadeId,
                HorasReais = p.HorasReais,
                MaterialReal = p.MaterialReal,
                Pago = p.Pago,
                MetodoPagamento = p.MetodoPagamento,
                Entrega = p.Entrega,
                ValorFrete = p.ValorFrete,
                LucroDesejadoPercentual = p.LucroDesejadoPercentual,
                ValorHoraMaquina = p.ValorHoraMaquina,
                ClienteIds = p.ProjetoClientes!.Select(pc => pc.ClienteId).ToList(),
                Materiais = p.ProjetoMateriais!.Select(pm => new ProjetoMaterialViewModel
                {
                    MaterialId = pm.MaterialId,
                    Quantidade = pm.Quantidade
                }).ToList(),
                Embalagens = (p.ProjetoEmbalagens ?? Enumerable.Empty<ProjetoEmbalagem>()).Select(pe => new ProjetoEmbalagemViewModel
                {
                    EmbalagemId = pe.EmbalagemId,
                    Quantidade = pe.Quantidade
                }).ToList()
            };

            await PreencherDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(ProjetoViewModel vm)
        {
            var p = await Carregar(vm.Id);
            if (p == null) return NotFound();

            if (vm.ClienteIds == null || vm.ClienteIds.Count == 0)
                ModelState.AddModelError(nameof(vm.ClienteIds), "Selecione pelo menos um cliente.");
            else
            {
                var clientesValidos = await _db.Clientes.Where(c => c.Ativo && vm.ClienteIds.Contains(c.Id)).CountAsync();
                if (clientesValidos != vm.ClienteIds.Count)
                    ModelState.AddModelError(nameof(vm.ClienteIds), "Um dos clientes selecionados é inválido.");
            }

            var materiaisEscolhidos = vm.Materiais?
                .Where(m => m.MaterialId > 0 && m.Quantidade > 0)
                .ToList() ?? new List<ProjetoMaterialViewModel>();
            if (materiaisEscolhidos.Count == 0)
                ModelState.AddModelError(nameof(vm.Materiais), "Selecione pelo menos um material com quantidade.");

            var embalagensEscolhidas = vm.Embalagens?
                .Where(e => e.EmbalagemId > 0 && e.Quantidade > 0)
                .GroupBy(e => e.EmbalagemId)
                .Select(g => new ProjetoEmbalagemViewModel { EmbalagemId = g.Key, Quantidade = g.Sum(x => x.Quantidade) })
                .ToList() ?? new List<ProjetoEmbalagemViewModel>();

            if (!ModelState.IsValid)
            {
                await PreencherDropdowns(vm);
                return View(vm);
            }

            p.Nome = vm.Nome;
            p.Descricao = vm.Descricao;
            p.GrupoId = vm.GrupoId;
            p.HorasEstimadas = vm.HorasEstimadas ?? 0;
            p.QtdMaterialEstimada = materiaisEscolhidos.Sum(m => m.Quantidade);
            p.Pintura = vm.Pintura;
            p.NivelDificuldadeId = vm.Pintura ? vm.NivelDificuldadeId : null;
            p.HorasReais = vm.HorasReais ?? 0;
            p.MaterialReal = vm.MaterialReal ?? 0;
            p.Pago = vm.Pago;
            p.MetodoPagamento = vm.MetodoPagamento;
            p.Entrega = vm.Entrega;
            p.ValorFrete = vm.Entrega ? (vm.ValorFrete ?? 0) : 0;
            p.LucroDesejadoPercentual = vm.LucroDesejadoPercentual;
            p.ValorHoraMaquina = vm.ValorHoraMaquina ?? 0;

            // Atualiza clientes (remover e adicionar)
            var pcAntigos = await _db.ProjetoClientes.Where(x => x.ProjetoId == p.Id).ToListAsync();
            _db.ProjetoClientes.RemoveRange(pcAntigos);
            foreach (var cid in vm.ClienteIds!)
                _db.ProjetoClientes.Add(new ProjetoCliente { ProjetoId = p.Id, ClienteId = cid });

            // Atualiza materiais
            var pmAntigos = await _db.ProjetoMateriais.Where(x => x.ProjetoId == p.Id).ToListAsync();
            _db.ProjetoMateriais.RemoveRange(pmAntigos);
            foreach (var m in materiaisEscolhidos)
                _db.ProjetoMateriais.Add(new ProjetoMaterial { ProjetoId = p.Id, MaterialId = m.MaterialId, Quantidade = m.Quantidade });

            // Atualiza embalagens
            var peAntigos = await _db.ProjetoEmbalagens.Where(x => x.ProjetoId == p.Id).ToListAsync();
            _db.ProjetoEmbalagens.RemoveRange(peAntigos);
            foreach (var e in embalagensEscolhidas)
                _db.ProjetoEmbalagens.Add(new ProjetoEmbalagem { ProjetoId = p.Id, EmbalagemId = e.EmbalagemId, Quantidade = e.Quantidade });

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Projeto atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Excluir

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var p = await _db.Projetos.FindAsync(id);
            if (p != null)
            {
                p.Ativo = false;
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Projeto excluído.";
            }
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Helpers

        private async Task<Projeto?> Carregar(int id)
        {
            return await _db.Projetos
                .Include(p => p.Grupo)
                .Include(p => p.NivelDificuldade)
                .Include(p => p.ProjetoClientes)!.ThenInclude(pc => pc.Cliente)
                .Include(p => p.ProjetoMateriais)!.ThenInclude(pm => pm.Material)
                .Include(p => p.ProjetoEmbalagens)!.ThenInclude(pe => pe.Embalagem)
                .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);
        }

        private async Task PreencherDropdowns(ProjetoViewModel vm)
        {
            vm.Grupos = await _db.Grupos.AsNoTracking().Where(g => g.Ativo)
                .Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Nome }).ToListAsync();
            vm.Clientes = await _db.Clientes.AsNoTracking().Where(c => c.Ativo)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.NomeCompleto }).OrderBy(x => x.Text).ToListAsync();
            vm.Niveis = await _db.NiveisDificuldade.AsNoTracking().Where(n => n.Ativo)
                .Select(n => new SelectListItem { Value = n.Id.ToString(), Text = n.Nome + " (" + n.LucroPercentual + "%)" }).OrderBy(x => x.Text).ToListAsync();
            var niveisPct = await _db.NiveisDificuldade.AsNoTracking().Where(n => n.Ativo)
                .Select(n => new { n.Id, n.LucroPercentual }).ToListAsync();
            vm.NiveisPct = niveisPct.ToDictionary(n => n.Id, n => n.LucroPercentual);
            var materiais = await _db.Materiais.AsNoTracking().Where(m => m.Ativo).ToListAsync();
            vm.MateriaisDisponiveis = materiais
                .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.TipoMaterial + " - " + m.Fornecedor + (string.IsNullOrEmpty(m.Cor) ? "" : " (" + m.Cor + ")") })
                .OrderBy(x => x.Text).ToList();
            vm.MateriaisPrecos = materiais.ToDictionary(m => m.Id.ToString(), m => m.PrecoPorGrama);

            var embalagens = await _db.Embalagens.AsNoTracking().Where(e => e.Ativo).ToListAsync();
            vm.EmbalagensDisponiveis = embalagens
                .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = $"{e.Nome} ({e.ValorUnitario:C})" })
                .OrderBy(x => x.Text).ToList();
            vm.EmbalagensPrecos = embalagens.ToDictionary(e => e.Id.ToString(), e => e.ValorUnitario);
        }

        private static decimal DecimalCfg(Dictionary<string, string?> cfg, string chave, decimal padrao)
            => decimal.TryParse(cfg.TryGetValue(chave, out var v) ? v : null,
                System.Globalization.CultureInfo.InvariantCulture, out var val) ? val : padrao;

        private ProjetoListaVM MontarVM(Projeto p)
        {
            var fin = FinanceCalc.Calcular(p);
            return new ProjetoListaVM
            {
                Projeto = p,
                Financeiro = fin,
                Clientes = p.ProjetoClientes!.Select(pc => pc.Cliente!.NomeCompleto).ToList(),
                Materiais = p.ProjetoMateriais!.Select(pm => $"{pm.Material!.TipoMaterial} ({pm.Quantidade:0.###} g)").ToList(),
                Embalagens = (p.ProjetoEmbalagens ?? Enumerable.Empty<ProjetoEmbalagem>())
                    .Where(pe => pe.Embalagem != null)
                    .Select(pe => $"{pe.Embalagem!.Nome} ({pe.Quantidade:0.###} un - {(pe.Embalagem.ValorUnitario * pe.Quantidade):C})")
                    .ToList()
            };
        }

        #endregion
    }

    public class ProjetoListaVM
    {
        public Projeto Projeto { get; set; } = new();
        public ProjetoFinanceiro Financeiro { get; set; } = new();
        public List<string> Clientes { get; set; } = new();
        public List<string> Materiais { get; set; } = new();
        public List<string> Embalagens { get; set; } = new();
    }
}
