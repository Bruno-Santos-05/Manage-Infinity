using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    /// <summary>
    /// Origem de contato/divulgação ("Nos conheceu por onde?") — similar aos grupos.
    /// Usado para cadastrar os meios pelos quais os clientes encontram o negócio.
    /// </summary>
    public class Origem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome da origem.")]
        [Display(Name = "Origem")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        public bool Ativo { get; set; } = true;

        public ICollection<Cliente>? Clientes { get; set; }
    }
}
