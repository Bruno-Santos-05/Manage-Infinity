using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    /// <summary>
    /// Configurações globais do sistema (chave/valor).
    /// </summary>
    public class Configuracao
    {
        public int Id { get; set; }

        [Required]
        public string Chave { get; set; } = string.Empty;

        public string? Valor { get; set; }

        public string Descricao { get; set; } = string.Empty;
    }
}
