using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    public class Grupo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome do grupo.")]
        [Display(Name = "Nome do Grupo")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        public bool Ativo { get; set; } = true;

        public ICollection<Projeto>? Projetos { get; set; }
    }
}
