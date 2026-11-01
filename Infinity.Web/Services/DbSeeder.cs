using Infinity.Web.Helpers;
using Infinity.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.Web.Services
{
    /// <summary>
    /// Garante a criação do banco SQLite (um único arquivo portátil) —
    /// caso os scripts SQL não tenham sido executados — e insere dados iniciais
    /// de forma idempotente (ADMIN, níveis de pintura, configurações, origens).
    /// </summary>
    public static class DbSeeder
    {
        public static void Initialize(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // SQLite: garante que a pasta do arquivo do banco exista antes de criar.
            // (O arquivo fica em Infinity.Web\Data\infinity.db — um único arquivo portátil.)
            var connString = db.Database.GetDbConnection().ConnectionString ?? "";
            string? dataSource = null;
            foreach (var parte in connString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                if (parte.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                    dataSource = parte.Substring(parte.IndexOf('=') + 1).Trim();
            }
            if (!string.IsNullOrWhiteSpace(dataSource) && !dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                var caminhoCompleto = Path.GetFullPath(dataSource);
                var pasta = Path.GetDirectoryName(caminhoCompleto);
                if (!string.IsNullOrEmpty(pasta) && !Directory.Exists(pasta))
                    Directory.CreateDirectory(pasta);
            }

            // Cria o banco e as tabelas se não existirem.
            db.Database.EnsureCreated();

            // ==================== MIGRAÇÕES DE ESQUEMA (idempotentes) ====================
            // O EnsureCreated NÃO altera tabelas já existentes. Para bancos criados antes de
            // determinadas versões, adicionamos as colunas novas de forma idempotente aqui.
            var connMig = db.Database.GetDbConnection();
            connMig.Open();
            try
            {
                // 1) Coluna PesoCompra (gramas) em Materiais — versões antigas não têm.
                if (!TemColuna(connMig, "Materiais", "PesoCompra"))
                {
                    using var cmd = connMig.CreateCommand();
                    cmd.CommandText = "ALTER TABLE 'Materiais' ADD COLUMN 'PesoCompra' TEXT NOT NULL DEFAULT '1000';";
                    cmd.ExecuteNonQuery();
                }

                // 2) Coluna Descricao em Projetos — versões antigas não têm.
                if (!TemColuna(connMig, "Projetos", "Descricao"))
                {
                    using var cmd = connMig.CreateCommand();
                    cmd.CommandText = "ALTER TABLE 'Projetos' ADD COLUMN 'Descricao' TEXT NULL;";
                    cmd.ExecuteNonQuery();
                }

                // 3) Tabelas Embalagens e ProjetoEmbalagens — bancos antigos não têm.
                using (var cmdEmb = connMig.CreateCommand())
                {
                    cmdEmb.CommandText = @"
                        CREATE TABLE IF NOT EXISTS 'Embalagens' (
                            'Id' INTEGER NOT NULL CONSTRAINT 'PK_Embalagens' PRIMARY KEY AUTOINCREMENT,
                            'Nome' TEXT NOT NULL,
                            'ValorUnitario' TEXT NOT NULL DEFAULT '0.00',
                            'Ativo' INTEGER NOT NULL DEFAULT 1
                        );
                        CREATE TABLE IF NOT EXISTS 'ProjetoEmbalagens' (
                            'ProjetoId' INTEGER NOT NULL,
                            'EmbalagemId' INTEGER NOT NULL,
                            'Quantidade' TEXT NOT NULL DEFAULT '1.00',
                            CONSTRAINT 'PK_ProjetoEmbalagens' PRIMARY KEY ('ProjetoId', 'EmbalagemId'),
                            CONSTRAINT 'FK_ProjetoEmbalagens_Projetos_ProjetoId' FOREIGN KEY ('ProjetoId') REFERENCES 'Projetos' ('Id') ON DELETE CASCADE,
                            CONSTRAINT 'FK_ProjetoEmbalagens_Embalagens_EmbalagemId' FOREIGN KEY ('EmbalagemId') REFERENCES 'Embalagens' ('Id') ON DELETE CASCADE
                        );";
                    cmdEmb.ExecuteNonQuery();
                }
            }
            finally
            {
                connMig.Close();
            }
            // ==================== FIM MIGRAÇÕES ====================

            // SQLite: mantém o banco em UM ÚNICO ARQUIVO usando journal_mode=DELETE
            // (rollback journal) em vez do padrão WAL. Assim não surgem arquivos
            // extras (.db-wal/.db-shm), facilitando copiar/colar o infinity.db.
            try
            {
                var conn = db.Database.GetDbConnection();
                conn.Open();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "PRAGMA journal_mode=DELETE;";
                    cmd.ExecuteNonQuery();
                }
                finally
                {
                    conn.Close();
                }
            }
            catch
            {
                // Falha de PRAGMA não impede o funcionamento; apenas avisa.
            }

            // Remove o usuário padrão inseguro (ADMIN) caso ainda exista no banco
            var adminAntigo = db.Usuarios.FirstOrDefault(u => u.Login == "ADMIN");
            if (adminAntigo != null)
            {
                db.Usuarios.Remove(adminAntigo);
            }

            // Garante o usuário principal configurado (pitocoepotter)
            var hashSenha = Cripto.HashSha256("zivkym-gujbe3-Wabfuw");
            var usuarioPrincipal = db.Usuarios.FirstOrDefault(u => u.Login == "pitocoepotter");
            if (usuarioPrincipal == null)
            {
                db.Usuarios.Add(new Usuario
                {
                    Nome = "Administrador",
                    Login = "pitocoepotter",
                    Senha = hashSenha,
                    Ativo = true
                });
            }
            else
            {
                usuarioPrincipal.Senha = hashSenha;
                usuarioPrincipal.Ativo = true;
            }

            // Níveis de dificuldade de pintura padrão
            if (!db.NiveisDificuldade.Any())
            {
                db.NiveisDificuldade.AddRange(
                    new NivelDificuldade { Nome = "Fácil", Descricao = "Pintura simples, poucos detalhes.", LucroPercentual = 10 },
                    new NivelDificuldade { Nome = "Médio", Descricao = "Pintura com detalhes moderados.", LucroPercentual = 20 },
                    new NivelDificuldade { Nome = "Difícil", Descricao = "Pintura com muitos detalhes.", LucroPercentual = 35 },
                    new NivelDificuldade { Nome = "Complexo", Descricao = "Pintura profissional/avançada.", LucroPercentual = 50 }
                );
            }

            // Configurações financeiras padrão
            if (!db.Configuracoes.Any())
            {
                db.Configuracoes.AddRange(
                    new Configuracao { Chave = "LucroDesejadoPadrao", Valor = "30", Descricao = "Percentual de lucro desejado padrão." },
                    new Configuracao { Chave = "ValorHoraMaquinaPadrao", Valor = "50", Descricao = "Valor padrão da hora máquina." },
                    new Configuracao { Chave = "MetodoPagamentoPadrao", Valor = "PIX", Descricao = "Método de pagamento padrão." }
                );
            }

            // Origens padrão ("Nos conheceu por onde?")
            if (!db.Origens.Any())
            {
                db.Origens.AddRange(
                    new Origem { Nome = "Instagram", Ativo = true },
                    new Origem { Nome = "Facebook", Ativo = true },
                    new Origem { Nome = "WhatsApp", Ativo = true },
                    new Origem { Nome = "Indicação", Ativo = true },
                    new Origem { Nome = "Google", Ativo = true },
                    new Origem { Nome = "TikTok", Ativo = true }
                );
            }

            db.SaveChanges();
        }

        /// <summary>
        /// Verifica se uma tabela possui determinada coluna (usado nas migrações idempotentes).
        /// </summary>
        private static bool TemColuna(System.Data.Common.DbConnection conn, string tabela, string coluna)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info('{tabela}');";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                if (string.Equals(rd.GetString(1), coluna, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
