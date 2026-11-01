using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    public class Material
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o fornecedor.")]
        [Display(Name = "Fornecedor")]
        public string Fornecedor { get; set; } = string.Empty;

        [Display(Name = "Cor")]
        public string Cor { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o valor.")]
        [Display(Name = "Valor (R$)")]
        [DataType(DataType.Currency)]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "Informe a quantidade comprada em gramas.")]
        [Display(Name = "Quantidade Comprada (gramas)")]
        [Range(0.001, double.MaxValue, ErrorMessage = "A quantidade em gramas deve ser maior que zero.")]
        public decimal PesoCompra { get; set; } = 1000;

        /// <summary>
        /// Preço de 1 grama deste material (Valor ÷ Quantidade comprada em gramas).
        /// </summary>
        public decimal PrecoPorGrama => PesoCompra > 0 ? Valor / PesoCompra : 0;

        [Required(ErrorMessage = "Informe o tipo de material.")]
        [Display(Name = "Tipo de Material")]
        public string TipoMaterial { get; set; } = string.Empty;

        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        public bool Ativo { get; set; } = true;

        public ICollection<ProjetoMaterial>? ProjetoMateriais { get; set; }
    }
}
