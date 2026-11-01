using System;

namespace Infinity.Web.Models
{
    /// <summary>
    /// Resultado financeiro de um projeto: custos e lucro considerando os valores
    /// ESTIMADOS e os valores REAIS, além das diferenças entre estes.
    /// </summary>
    public class ProjetoFinanceiro
    {
        // ---------- Horas / material ----------
        public decimal HorasEstimadas { get; set; }
        public decimal HorasReais { get; set; }
        public decimal MaterialEstimado { get; set; }
        public decimal MaterialReal { get; set; }

        public decimal ValorHoraMaquina { get; set; }
        public decimal LucroDesejadoPercentual { get; set; }

        /// <summary>Percentual de lucro extra do nível de pintura (ex.: 10, 20, 35, 50).</summary>
        public decimal PinturaPercentual { get; set; }

        // ---------- Custos ESTIMADOS ----------
        public decimal CustoMaterialEstimado { get; set; }
        public decimal CustoHorasEstimado { get; set; }
        public decimal CustoPinturaEstimado { get; set; }
        public decimal CustoFreteEstimado { get; set; }
        public decimal CustoEmbalagemEstimado { get; set; }
        public decimal CustoTotalEstimado => CustoMaterialEstimado + CustoHorasEstimado + CustoPinturaEstimado + CustoFreteEstimado + CustoEmbalagemEstimado;

        // ---------- Custos REAIS ----------
        public decimal CustoMaterialReal { get; set; }
        public decimal CustoHorasReal { get; set; }
        public decimal CustoPinturaReal { get; set; }
        public decimal CustoFreteReal { get; set; }
        public decimal CustoEmbalagemReal { get; set; }
        public decimal CustoTotalReal => CustoMaterialReal + CustoHorasReal + CustoPinturaReal + CustoFreteReal + CustoEmbalagemReal;

        // ---------- Soma de custo de MATERIAL + HORA (estimado / real) ----------
        public decimal SomaMaterialHoraEstimado => CustoMaterialEstimado + CustoHorasEstimado;
        public decimal SomaMaterialHoraReal => CustoMaterialReal + CustoHorasReal;
        /// <summary>Diferença entre a soma (material + hora) real e a estimada.</summary>
        public decimal DiffSomaMaterialHora => SomaMaterialHoraReal - SomaMaterialHoraEstimado;

        /// <summary>
        /// Preço a cobrar (com base no custo estimado).
        /// Regra: aplica o lucro desejado sobre o custo total e, se houver pintura,
        /// acrescenta a porcentagem de lucro da pintura por cima do valor já com lucro.
        /// Ex.: custo 100, lucro desejado 50% → 150; pintura 50% → 225 (150 × 1,5).
        /// </summary>
        public decimal PrecoCobrar
        {
            get
            {
                decimal lucro = LucroDesejadoPercentual;
                decimal pinturaPct = PinturaPercentual;
                decimal comLucro = CustoTotalEstimado * (1 + lucro / 100m);
                decimal comPintura = comLucro * (1 + pinturaPct / 100m);
                return comPintura;
            }
        }

        /// <summary>Preço cobrado (baseado na estimativa, com lucro + lucro da pintura).</summary>
        public decimal ValorVenda => PrecoCobrar;

        /// <summary>Indica se há dados REAIS de consumo preenchidos (material ou horas).</summary>
        public bool TemDadosReais => MaterialReal > 0 || HorasReais > 0;

        /// <summary>Lucro esperado (baseado na estimativa): valor de venda - custo estimado.</summary>
        public decimal LucroEsperado => ValorVenda - CustoTotalEstimado;

        /// <summary>
        /// Lucro real: valor de venda - custo real.
        /// Só é calculado se houver dados reais (MaterialReal ou HorasReais). Caso contrário fica 0,
        /// pois ainda não há informação real para afirmar lucro (não usar o custo estimado como real).
        /// </summary>
        public decimal LucroReal => TemDadosReais ? ValorVenda - CustoTotalReal : 0m;

        /// <summary>Alias de compatibilidade para o lucro estimado.</summary>
        public decimal LucroEstimado => LucroEsperado;

        // ---------- Diferenças (real - estimado) ----------
        public decimal DiffHoras => HorasReais - HorasEstimadas;
        public decimal DiffMaterial => MaterialReal - MaterialEstimado;
        public decimal DiffCusto => CustoTotalReal - CustoTotalEstimado;
        public decimal DiffLucro => LucroReal - LucroEstimado;
        public decimal DiffCustoMaterial => CustoMaterialReal - CustoMaterialEstimado;
        public decimal DiffCustoHoras => CustoHorasReal - CustoHorasEstimado;
        public decimal? PinturaSomada { get; set; }
    }
}
