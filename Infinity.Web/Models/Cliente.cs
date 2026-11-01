using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome completo.")]
        [Display(Name = "Nome Completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Display(Name = "Número Celular")]
        public string? NumeroCelular { get; set; }

        [Display(Name = "Nos conheceu por onde?")]
        public int? OrigemId { get; set; }
        public Origem? Origem { get; set; }

        [Display(Name = "OBS")]
        [DataType(DataType.MultilineText)]
        public string? Obs { get; set; }

        public bool Ativo { get; set; } = true;

        public ICollection<ProjetoCliente>? ProjetoClientes { get; set; }
    }
}
