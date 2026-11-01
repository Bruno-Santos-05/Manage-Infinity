using System.ComponentModel.DataAnnotations;

namespace Infinity.Web.Models
{
    public class ProjetoMaterial
    {
        public int ProjetoId { get; set; }
        public Projeto? Projeto { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        [Display(Name = "Quantidade")]
        public decimal Quantidade { get; set; } = 1;
    }
}
