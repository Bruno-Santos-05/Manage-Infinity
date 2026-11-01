/* ============================================================================
   SISTEMA INFINITY - Script de criação do Banco de Dados (SQL Server)
   ----------------------------------------------------------------------------
   OBS: Troque o caminho do arquivo (.mdf / .ldf) abaixo conforme sua máquina,
   ou simplesmente remova o bloco IF NOT EXISTS / CREATE DATABASE para apontar
   para um banco já existente.
   ============================================================================ */

IF DB_ID(N'InfinitySistema') IS NULL
BEGIN
    CREATE DATABASE [InfinitySistema]
END
GO

USE [InfinitySistema]
GO

-- ============================================================================
-- 1. USUÁRIOS (login/senha)
-- ============================================================================
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL DROP TABLE dbo.Usuarios;
GO
CREATE TABLE dbo.Usuarios
(
    Id      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nome    NVARCHAR(150)     NOT NULL,
    Login   NVARCHAR(50)      NOT NULL UNIQUE,
    Senha   NVARCHAR(500)     NOT NULL,  -- hash SHA256
    Ativo   BIT               NOT NULL DEFAULT 1
);
GO

-- ============================================================================
-- 2. MATERIAIS
-- ============================================================================
IF OBJECT_ID(N'dbo.Materiais', N'U') IS NOT NULL DROP TABLE dbo.Materiais;
GO
CREATE TABLE dbo.Materiais
(
    Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Fornecedor   NVARCHAR(150)     NOT NULL,
    Cor          NVARCHAR(50)      NULL,
    Valor        DECIMAL(18,2)     NOT NULL DEFAULT 0,
    TipoMaterial NVARCHAR(100)     NOT NULL,
    Descricao    NVARCHAR(500)     NULL,
    Ativo        BIT               NOT NULL DEFAULT 1
);
GO

-- ============================================================================
-- 3. ORIGENS ("Nos conheceu por onde?")
-- ============================================================================
IF OBJECT_ID(N'dbo.Origens', N'U') IS NOT NULL DROP TABLE dbo.Origens;
GO
CREATE TABLE dbo.Origens
(
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nome        NVARCHAR(150)     NOT NULL,
    Descricao   NVARCHAR(500)     NULL,
    Ativo       BIT               NOT NULL DEFAULT 1
);
GO

-- ============================================================================
-- 3.1 CLIENTES
-- ============================================================================
IF OBJECT_ID(N'dbo.Clientes', N'U') IS NOT NULL DROP TABLE dbo.Clientes;
GO
CREATE TABLE dbo.Clientes
(
    Id            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NomeCompleto  NVARCHAR(200)     NOT NULL,
    NumeroCelular NVARCHAR(30)      NULL,
    OrigemId      INT               NULL,   -- "Nos conheceu por onde?" (tabela Origens)
    Obs           NVARCHAR(MAX)     NULL,
    Ativo         BIT               NOT NULL DEFAULT 1,
    CONSTRAINT FK_Clientes_Origens FOREIGN KEY (OrigemId) REFERENCES dbo.Origens(Id) ON DELETE SET NULL
);
GO

-- ============================================================================
-- 4. GRUPOS DE PROJETO
-- ============================================================================
IF OBJECT_ID(N'dbo.Grupos', N'U') IS NOT NULL DROP TABLE dbo.Grupos;
GO
CREATE TABLE dbo.Grupos
(
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nome        NVARCHAR(150)     NOT NULL,
    Descricao   NVARCHAR(500)     NULL,
    Ativo       BIT               NOT NULL DEFAULT 1
);
GO

-- ============================================================================
-- 5. NÍVEIS DE DIFICULDADE DE PINTURA (com % de lucro por nível)
-- ============================================================================
IF OBJECT_ID(N'dbo.NiveisDificuldade', N'U') IS NOT NULL DROP TABLE dbo.NiveisDificuldade;
GO
CREATE TABLE dbo.NiveisDificuldade
(
    Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nome            NVARCHAR(150)     NOT NULL,
    Descricao       NVARCHAR(500)     NULL,
    LucroPercentual DECIMAL(18,2)     NOT NULL DEFAULT 0,
    Ativo           BIT               NOT NULL DEFAULT 1
);
GO

-- ============================================================================
-- 6. CONFIGURAÇÕES GLOBAIS (chave/valor)
-- ============================================================================
IF OBJECT_ID(N'dbo.Configuracoes', N'U') IS NOT NULL DROP TABLE dbo.Configuracoes;
GO
CREATE TABLE dbo.Configuracoes
(
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Chave       NVARCHAR(100)     NOT NULL UNIQUE,
    Valor       NVARCHAR(500)     NULL,
    Descricao   NVARCHAR(300)     NOT NULL
);
GO

-- ============================================================================
-- 7. PROJETOS
-- ============================================================================
IF OBJECT_ID(N'dbo.Projetos', N'U') IS NOT NULL DROP TABLE dbo.Projetos;
GO
CREATE TABLE dbo.Projetos
(
    Id                         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nome                       NVARCHAR(200)     NOT NULL,
    GrupoId                    INT               NULL,
    HorasEstimadas             DECIMAL(18,2)     NOT NULL DEFAULT 0,
    QtdMaterialEstimada        DECIMAL(18,2)     NOT NULL DEFAULT 0,
    Pintura                    BIT               NOT NULL DEFAULT 0,
    NivelDificuldadeId         INT               NULL,
    HorasReais                 DECIMAL(18,2)     NOT NULL DEFAULT 0,
    MaterialReal               DECIMAL(18,2)     NOT NULL DEFAULT 0,
    Pago                       BIT               NOT NULL DEFAULT 0,
    MetodoPagamento            NVARCHAR(30)      NULL,          -- CARTÃO / PIX / DINHEIRO
    Entrega                    BIT               NOT NULL DEFAULT 0,
    ValorFrete                 DECIMAL(18,2)     NOT NULL DEFAULT 0,
    LucroDesejadoPercentual    DECIMAL(18,2)     NULL,          -- % (config global também existe)
    ValorHoraMaquina           DECIMAL(18,2)     NOT NULL DEFAULT 0,
    Ativo                      BIT               NOT NULL DEFAULT 1,
    DataCriacao                DATETIME2(7)      NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Projetos_Grupos            FOREIGN KEY (GrupoId)            REFERENCES dbo.Grupos(Id)             ON DELETE RESTRICT,
    CONSTRAINT FK_Projetos_NiveisDificuldade FOREIGN KEY (NivelDificuldadeId) REFERENCES dbo.NiveisDificuldade(Id) ON DELETE SET NULL
);
GO

-- ============================================================================
-- 8. RELAÇÃO PROJETO X CLIENTE (N para N)
-- ============================================================================
IF OBJECT_ID(N'dbo.ProjetoClientes', N'U') IS NOT NULL DROP TABLE dbo.ProjetoClientes;
GO
CREATE TABLE dbo.ProjetoClientes
(
    ProjetoId INT NOT NULL,
    ClienteId INT NOT NULL,
    CONSTRAINT PK_ProjetoClientes PRIMARY KEY (ProjetoId, ClienteId),
    CONSTRAINT FK_ProjetoClientes_Projeto FOREIGN KEY (ProjetoId) REFERENCES dbo.Projetos(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProjetoClientes_Cliente FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes(Id) ON DELETE CASCADE
);
GO

-- ============================================================================
-- 9. RELAÇÃO PROJETO X MATERIAL (N para N, com quantidade)
-- ============================================================================
IF OBJECT_ID(N'dbo.ProjetoMateriais', N'U') IS NOT NULL DROP TABLE dbo.ProjetoMateriais;
GO
CREATE TABLE dbo.ProjetoMateriais
(
    ProjetoId  INT           NOT NULL,
    MaterialId INT           NOT NULL,
    Quantidade DECIMAL(18,2) NOT NULL DEFAULT 1,
    CONSTRAINT PK_ProjetoMateriais PRIMARY KEY (ProjetoId, MaterialId),
    CONSTRAINT FK_ProjetoMateriais_Projeto FOREIGN KEY (ProjetoId) REFERENCES dbo.Projetos(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProjetoMateriais_Material FOREIGN KEY (MaterialId) REFERENCES dbo.Materiais(Id) ON DELETE CASCADE
);
GO

-- ============================================================================
-- 10. SEED (dados iniciais)
-- ============================================================================

-- Usuário padrão: ADMIN / ADMIN  (senha gravada como hash SHA256 de "admin")
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Login = 'ADMIN')
BEGIN
    INSERT INTO dbo.Usuarios (Nome, Login, Senha, Ativo)
    VALUES (N'Administrador', N'ADMIN', N'8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 1);
END
GO

-- Exemplos de níveis de dificuldade de pintura
IF NOT EXISTS (SELECT 1 FROM dbo.NiveisDificuldade)
BEGIN
    INSERT INTO dbo.NiveisDificuldade (Nome, Descricao, LucroPercentual)
    VALUES
        (N'Fácil',    N'Pintura simples, poucos detalhes.', 10),
        (N'Médio',    N'Pintura com detalhes moderados.',    20),
        (N'Difícil',  N'Pintura com muitos detalhes.',       35),
        (N'Complexo', N'Pintura profissional/avançada.',     50);
END
GO

-- Configurações globais padrão
IF NOT EXISTS (SELECT 1 FROM dbo.Configuracoes)
BEGIN
    INSERT INTO dbo.Configuracoes (Chave, Valor, Descricao)
    VALUES
        (N'LucroDesejadoPadrao', N'30',        N'Percentual de lucro desejado padrão para novos projetos.'),
        (N'ValorHoraMaquinaPadrao', N'50',     N'Valor padrão da hora máquina para novos projetos.'),
        (N'MetodoPagamentoPadrao', N'PIX',     N'Método de pagamento padrão (CARTÃO / PIX / DINHEIRO).');
END
GO

-- Origens padrão ("Nos conheceu por onde?")
IF NOT EXISTS (SELECT 1 FROM dbo.Origens)
BEGIN
    INSERT INTO dbo.Origens (Nome, Descricao, Ativo)
    VALUES
        (N'Instagram',  N'Redes sociais - Instagram.',      1),
        (N'Facebook',   N'Redes sociais - Facebook.',       1),
        (N'WhatsApp',   N'Contato direto via WhatsApp.',    1),
        (N'Indicação',  N'Indicado por outro cliente.',     1),
        (N'Google',     N'Buscas no Google.',               1),
        (N'TikTok',     N'Redes sociais - TikTok.',         1);
END
GO

PRINT N'Banco de dados InfinitySistema criado com sucesso!'
GO
