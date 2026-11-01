using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    public class Projeto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome do projeto.")]
        [Display(Name = "Nome do Projeto")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Descrição do Projeto")]
        [DataType(DataType.MultilineText)]
        public string? Descricao { get; set; }

        [Display(Name = "Grupo do Projeto")]
        public int? GrupoId { get; set; }
        public Grupo? Grupo { get; set; }

        // ---- Estimativa ----
        [Display(Name = "Horas de Impressão Estimadas")]
        [DataType(DataType.Currency)]
        public decimal HorasEstimadas { get; set; }

        [Display(Name = "Quantidade de Material Estimada")]
        public decimal QtdMaterialEstimada { get; set; }

        // ---- Pintura ----
        [Display(Name = "Pintura")]
        public bool Pintura { get; set; }

        [Display(Name = "Nível de Dificuldade da Pintura")]
        public int? NivelDificuldadeId { get; set; }
        public NivelDificuldade? NivelDificuldade { get; set; }

        // ---- Valores reais ----
        [Display(Name = "Quantidade de Horas Real")]
        public decimal HorasReais { get; set; }

        [Display(Name = "Quantidade de Material Real")]
        public decimal MaterialReal { get; set; }

        // ---- Pagamento ----
        [Display(Name = "Pago")]
        public bool Pago { get; set; }

        [Display(Name = "Método de Pagamento")]
        public string? MetodoPagamento { get; set; } // CARTÃO / PIX / DINHEIRO

        // ---- Entrega ----
        [Display(Name = "Entrega")]
        public bool Entrega { get; set; }

        [Display(Name = "Valor do Frete (R$)")]
        [DataType(DataType.Currency)]
        public decimal ValorFrete { get; set; }

        // ---- Configuração financeira ----
        [Display(Name = "Lucro Desejado (%)")]
        public decimal? LucroDesejadoPercentual { get; set; }

        [Display(Name = "Valor da Hora Máquina (R$)")]
        [DataType(DataType.Currency)]
        public decimal ValorHoraMaquina { get; set; }

        // ---- Status / dados ----
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        // ---- Relacionamentos ----
        public ICollection<ProjetoCliente>? ProjetoClientes { get; set; }
        public ICollection<ProjetoMaterial>? ProjetoMateriais { get; set; }
        public ICollection<ProjetoEmbalagem>? ProjetoEmbalagens { get; set; }
    }
}
