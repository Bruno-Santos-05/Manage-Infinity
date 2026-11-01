namespace Infinity.Web.Models
{
    /// <summary>
    /// Centraliza o cálculo financeiro do projeto considerando estimativas e valores reais.
    /// </summary>
    public static class FinanceCalc
    {
        private const decimal PercentualHorasPintura = 0.15m; // 15% do tempo é destinado à pintura

        public static ProjetoFinanceiro Calcular(Projeto p)
        {
            var fin = new ProjetoFinanceiro
            {
                HorasEstimadas = p.HorasEstimadas,
                HorasReais = p.HorasReais,
                MaterialReal = p.MaterialReal,
                ValorHoraMaquina = p.ValorHoraMaquina,
                LucroDesejadoPercentual = p.LucroDesejadoPercentual ?? 0
            };

            // ---- Quantidade de material estimado = soma das quantidades (em gramas) dos materiais adicionados ----
            decimal qtdMateriais = 0m;
            if (p.ProjetoMateriais != null)
                qtdMateriais = p.ProjetoMateriais.Sum(pm => pm.Quantidade);
            fin.MaterialEstimado = qtdMateriais;

            // ---- Custos ESTIMADOS ----
            // Custo do material no projeto = (gramas gastas ÷ gramas compradas) × valor do material
            // = gramas gastas × preço por grama.
            if (p.ProjetoMateriais != null)
                fin.CustoMaterialEstimado = p.ProjetoMateriais.Sum(pm => (pm.Material?.PrecoPorGrama ?? 0) * pm.Quantidade);

            fin.CustoHorasEstimado = p.HorasEstimadas * p.ValorHoraMaquina;

            if (p.Pintura && p.NivelDificuldade != null)
            {
                fin.CustoPinturaEstimado = p.HorasEstimadas * PercentualHorasPintura * p.ValorHoraMaquina;
                fin.PinturaSomada = p.NivelDificuldade.LucroPercentual;
                fin.PinturaPercentual = p.NivelDificuldade.LucroPercentual;
            }

            fin.CustoFreteEstimado = p.Entrega ? p.ValorFrete : 0;

            decimal custoEmbalagem = 0m;
            if (p.ProjetoEmbalagens != null)
                custoEmbalagem = p.ProjetoEmbalagens.Sum(pe => (pe.Embalagem?.ValorUnitario ?? 0) * pe.Quantidade);
            fin.CustoEmbalagemEstimado = custoEmbalagem;

            // ---- Custos REAIS ----
            // Material real é proporcional à quantidade realmente consumida.
            // Regra: o custo real só é calculado se houver informação real preenchida
            // (MaterialReal > 0). Caso contrário fica 0 — NÃO usa o estimado como "real".
            if (qtdMateriais > 0 && p.MaterialReal > 0)
                fin.CustoMaterialReal = fin.CustoMaterialEstimado * (p.MaterialReal / qtdMateriais);
            else
                fin.CustoMaterialReal = 0m;

            fin.CustoHorasReal = p.HorasReais * p.ValorHoraMaquina;

            if (p.Pintura && p.NivelDificuldade != null)
                fin.CustoPinturaReal = p.HorasReais * PercentualHorasPintura * p.ValorHoraMaquina;

            fin.CustoFreteReal = p.Entrega ? p.ValorFrete : 0;
            fin.CustoEmbalagemReal = custoEmbalagem;

            return fin;
        }
    }
}
