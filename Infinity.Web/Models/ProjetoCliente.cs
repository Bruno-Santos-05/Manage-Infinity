namespace Infinity.Web.Models
{
    public class ProjetoCliente
    {
        public int ProjetoId { get; set; }
        public Projeto? Projeto { get; set; }

        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
    }
}
