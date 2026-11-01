using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    public class ProjetoEmbalagem
    {
        public int ProjetoId { get; set; }
        public Projeto? Projeto { get; set; }

        public int EmbalagemId { get; set; }
        public Embalagem? Embalagem { get; set; }

        [Display(Name = "Quantidade")]
        public decimal Quantidade { get; set; } = 1;
    }
}
