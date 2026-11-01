using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o login.")]
        [Display(Name = "Login")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;
    }
}
