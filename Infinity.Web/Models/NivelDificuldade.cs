using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    public class NivelDificuldade
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome do nível.")]
        [Display(Name = "Nível de Dificuldade")]
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        [Required(ErrorMessage = "Informe o percentual de lucro.")]
        [Display(Name = "Lucro (%)")]
        [Range(0, 10000, ErrorMessage = "Informe um percentual válido.")]
        public decimal LucroPercentual { get; set; }

        public bool Ativo { get; set; } = true;

        public ICollection<Projeto>? Projetos { get; set; }
    }
}
