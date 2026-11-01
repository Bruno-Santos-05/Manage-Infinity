using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infinity.Web.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Material> Materiais { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<Origem> Origens { get; set; }
        public DbSet<NivelDificuldade> NiveisDificuldade { get; set; }
        public DbSet<Projeto> Projetos { get; set; }
        public DbSet<ProjetoCliente> ProjetoClientes { get; set; }
        public DbSet<ProjetoMaterial> ProjetoMateriais { get; set; }
        public DbSet<Embalagem> Embalagens { get; set; }
        public DbSet<ProjetoEmbalagem> ProjetoEmbalagens { get; set; }
        public DbSet<Configuracao> Configuracoes { get; set; }

        /// <summary>
        /// Converter p/ preservar a precisão decimal no SQLite (armazenado como texto).
        /// Importante pois os valores são financeiros (R$).
        /// </summary>
        private static readonly ValueConverter<decimal, string> DecimalTexto =
            new(d => d.ToString("0.00####", System.Globalization.CultureInfo.InvariantCulture),
                s => decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture));

        private static readonly ValueConverter<decimal?, string?> DecimalNuloTexto =
            new(d => d.HasValue ? d.Value.ToString("0.00####", System.Globalization.CultureInfo.InvariantCulture) : null,
                s => string.IsNullOrEmpty(s) ? (decimal?)null : decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usuario
            modelBuilder.Entity<Usuario>().ToTable("Usuarios");
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Login).IsUnique();

            // Material
            modelBuilder.Entity<Material>().ToTable("Materiais");
            modelBuilder.Entity<Material>().Property(m => m.Valor).HasConversion(DecimalTexto);

            // Cliente
            modelBuilder.Entity<Cliente>().ToTable("Clientes");
            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.Origem)
                .WithMany(o => o.Clientes)
                .HasForeignKey(c => c.OrigemId)
                .OnDelete(DeleteBehavior.SetNull);

            // Origem
            modelBuilder.Entity<Origem>().ToTable("Origens");

            // Grupo
            modelBuilder.Entity<Grupo>().ToTable("Grupos");

            // NivelDificuldade
            modelBuilder.Entity<NivelDificuldade>().ToTable("NiveisDificuldade");
            modelBuilder.Entity<NivelDificuldade>()
                .Property(n => n.LucroPercentual).HasConversion(DecimalTexto);

            // Projeto
            modelBuilder.Entity<Projeto>().ToTable("Projetos");
            modelBuilder.Entity<Projeto>()
                .Property(p => p.HorasEstimadas).HasConversion(DecimalTexto);
            modelBuilder.Entity<Projeto>()
                .Property(p => p.QtdMaterialEstimada).HasConversion(DecimalTexto);
            modelBuilder.Entity<Projeto>()
                .Property(p => p.HorasReais).HasConversion(DecimalTexto);
            modelBuilder.Entity<Projeto>()
                .Property(p => p.MaterialReal).HasConversion(DecimalTexto);
            modelBuilder.Entity<Projeto>()
                .Property(p => p.ValorFrete).HasConversion(DecimalTexto);
            modelBuilder.Entity<Projeto>()
                .Property(p => p.LucroDesejadoPercentual).HasConversion(DecimalNuloTexto);
            modelBuilder.Entity<Projeto>()
                .Property(p => p.ValorHoraMaquina).HasConversion(DecimalTexto);

            modelBuilder.Entity<Projeto>()
                .HasOne(p => p.Grupo)
                .WithMany(g => g.Projetos)
                .HasForeignKey(p => p.GrupoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Projeto>()
                .HasOne(p => p.NivelDificuldade)
                .WithMany(n => n.Projetos)
                .HasForeignKey(p => p.NivelDificuldadeId)
                .OnDelete(DeleteBehavior.SetNull);

            // ProjetoCliente (N:N)
            modelBuilder.Entity<ProjetoCliente>().ToTable("ProjetoClientes");
            modelBuilder.Entity<ProjetoCliente>()
                .HasKey(pc => new { pc.ProjetoId, pc.ClienteId });

            modelBuilder.Entity<ProjetoCliente>()
                .HasOne(pc => pc.Projeto)
                .WithMany(p => p.ProjetoClientes)
                .HasForeignKey(pc => pc.ProjetoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjetoCliente>()
                .HasOne(pc => pc.Cliente)
                .WithMany(c => c.ProjetoClientes)
                .HasForeignKey(pc => pc.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProjetoMaterial (N:N)
            modelBuilder.Entity<ProjetoMaterial>().ToTable("ProjetoMateriais");
            modelBuilder.Entity<ProjetoMaterial>()
                .HasKey(pm => new { pm.ProjetoId, pm.MaterialId });

            modelBuilder.Entity<ProjetoMaterial>()
                .Property(pm => pm.Quantidade).HasConversion(DecimalTexto);

            modelBuilder.Entity<ProjetoMaterial>()
                .HasOne(pm => pm.Projeto)
                .WithMany(p => p.ProjetoMateriais)
                .HasForeignKey(pm => pm.ProjetoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjetoMaterial>()
                .HasOne(pm => pm.Material)
                .WithMany(m => m.ProjetoMateriais)
                .HasForeignKey(pm => pm.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            // Embalagem
            modelBuilder.Entity<Embalagem>().ToTable("Embalagens");
            modelBuilder.Entity<Embalagem>().Property(e => e.ValorUnitario).HasConversion(DecimalTexto);

            // ProjetoEmbalagem (N:N)
            modelBuilder.Entity<ProjetoEmbalagem>().ToTable("ProjetoEmbalagens");
            modelBuilder.Entity<ProjetoEmbalagem>()
                .HasKey(pe => new { pe.ProjetoId, pe.EmbalagemId });

            modelBuilder.Entity<ProjetoEmbalagem>()
                .Property(pe => pe.Quantidade).HasConversion(DecimalTexto);

            modelBuilder.Entity<ProjetoEmbalagem>()
                .HasOne(pe => pe.Projeto)
                .WithMany(p => p.ProjetoEmbalagens)
                .HasForeignKey(pe => pe.ProjetoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjetoEmbalagem>()
                .HasOne(pe => pe.Embalagem)
                .WithMany(e => e.ProjetoEmbalagens)
                .HasForeignKey(pe => pe.EmbalagemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
