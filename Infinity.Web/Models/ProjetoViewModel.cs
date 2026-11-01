using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    public class ProjetoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome do projeto.")]
        [Display(Name = "Nome do Projeto")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Descrição do Projeto")]
        public string? Descricao { get; set; }

        [Display(Name = "Grupo do Projeto")]
        public int? GrupoId { get; set; }

        // ---- Estimativa ----
        [Display(Name = "Horas de Impressão Estimadas")]
        public decimal? HorasEstimadas { get; set; }

        [Display(Name = "Quantidade de Material Estimada")]
        public decimal QtdMaterialEstimada { get; set; }

        // ---- Pintura ----
        [Display(Name = "Pintura")]
        public bool Pintura { get; set; }

        [Display(Name = "Nível de Dificuldade da Pintura")]
        public int? NivelDificuldadeId { get; set; }

        // ---- Valores reais ----
        [Display(Name = "Quantidade de Horas Real")]
        public decimal? HorasReais { get; set; }

        [Display(Name = "Quantidade de Material Real")]
        public decimal? MaterialReal { get; set; }

        // ---- Pagamento ----
        [Display(Name = "Pago")]
        public bool Pago { get; set; }

        [Display(Name = "Método de Pagamento")]
        public string? MetodoPagamento { get; set; }

        // ---- Entrega ----
        [Display(Name = "Entrega")]
        public bool Entrega { get; set; }

        [Display(Name = "Valor do Frete (R$)")]
        public decimal? ValorFrete { get; set; }

        // ---- Configuração financeira ----
        [Display(Name = "Lucro Desejado (%)")]
        public decimal? LucroDesejadoPercentual { get; set; }

        [Display(Name = "Valor da Hora Máquina (R$)")]
        public decimal? ValorHoraMaquina { get; set; }

        // ---- Seleções (N:N) ----
        public List<int> ClienteIds { get; set; } = new();
        public List<ProjetoMaterialViewModel> Materiais { get; set; } = new();
        public List<ProjetoEmbalagemViewModel> Embalagens { get; set; } = new();

        // ---- Dropdowns ----
        public List<SelectListItem> Grupos { get; set; } = new();
        public List<SelectListItem> Clientes { get; set; } = new();
        public List<SelectListItem> Niveis { get; set; } = new();
        public List<SelectListItem> MateriaisDisponiveis { get; set; } = new();
        public List<SelectListItem> EmbalagensDisponiveis { get; set; } = new();

        /// <summary>Preço (IdMaterial -> Valor) dos materiais, para cálculo automático no formulário.</summary>
        public Dictionary<string, decimal> MateriaisPrecos { get; set; } = new();

        /// <summary>Preço unitário (IdEmbalagem -> ValorUnitario) das embalagens, para cálculo automático no formulário.</summary>
        public Dictionary<string, decimal> EmbalagensPrecos { get; set; } = new();

        /// <summary>Percentual de lucro por nível de pintura (Id -> %), para cálculo do preço a cobrar ao vivo.</summary>
        public Dictionary<int, decimal> NiveisPct { get; set; } = new();
    }

    public class ProjetoMaterialViewModel
    {
        public int MaterialId { get; set; }
        public decimal Quantidade { get; set; } = 1;
    }

    public class ProjetoEmbalagemViewModel
    {
        public int EmbalagemId { get; set; }
        public decimal Quantidade { get; set; } = 1;
    }
}
