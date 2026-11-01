using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    public class Embalagem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome da embalagem.")]
        [Display(Name = "Nome da Embalagem")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Valor Unitário (R$)")]
        [DataType(DataType.Currency)]
        public decimal ValorUnitario { get; set; }

        public bool Ativo { get; set; } = true;

        public ICollection<ProjetoEmbalagem>? ProjetoEmbalagens { get; set; }
    }
}
