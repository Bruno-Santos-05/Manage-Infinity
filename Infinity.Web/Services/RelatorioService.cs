using ClosedXML.Excel;
using Infinity.Web.Models;

namespace Infinity.Web.Services
{
    /// <summary>
    /// Gera relatórios em Excel (xlsx) usando ClosedXML.
    /// </summary>
    public static class RelatorioService
    {
        /// <summary>
        /// Relatório financeiro completo dos projetos (estimado x real).
        /// </summary>
        public static byte[] ProjetosExcel(List<ProjetoFinanceiroVM> itens, string titulo, DateTime geradoEm)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Projetos");

            ws.Cell(1, 1).Value = titulo;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = "Gerado em: " + geradoEm.ToString("dd/MM/yyyy HH:mm");

            int r = 4;
            string[] headers = {
                "Projeto","Grupo","Clientes","Pago","Método","Pintura","Nível",
                "Horas Est.","Horas Real","Mat. Est.","Mat. Real",
                "Custo Mat. Est.","Custo Horas Est.","Custo Pint. Est.","Custo Frete Est.","Custo Total Est.",
                "Custo Mat. Real","Custo Horas Real","Custo Pint. Real","Custo Frete Real","Custo Total Real",
                "Valor Venda","Lucro Est.","Lucro Real","Dif. Custo","Dif. Lucro"
            };
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(r, c + 1).Value = headers[c];
                ws.Cell(r, c + 1).Style.Font.Bold = true;
                ws.Cell(r, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
                ws.Cell(r, c + 1).Style.Font.FontColor = XLColor.White;
            }

            int lin = r;
            foreach (var it in itens)
            {
                lin++;
                var f = it.Financeiro;
                ws.Cell(lin, 1).Value = it.Projeto.Nome;
                ws.Cell(lin, 2).Value = it.GrupoNome;
                ws.Cell(lin, 3).Value = string.Join(", ", it.Clientes);
                ws.Cell(lin, 4).Value = it.Projeto.Pago ? "SIM" : "NÃO";
                ws.Cell(lin, 5).Value = it.Projeto.MetodoPagamento ?? "";
                ws.Cell(lin, 6).Value = it.Projeto.Pintura ? "SIM" : "NÃO";
                ws.Cell(lin, 7).Value = it.NivelNome;

                ws.Cell(lin, 8).Value = (double)f.HorasEstimadas;
                ws.Cell(lin, 9).Value = (double)f.HorasReais;
                ws.Cell(lin, 10).Value = (double)f.MaterialEstimado;
                ws.Cell(lin, 11).Value = (double)f.MaterialReal;

                Num(ws, lin, 12, f.CustoMaterialEstimado);
                Num(ws, lin, 13, f.CustoHorasEstimado);
                Num(ws, lin, 14, f.CustoPinturaEstimado);
                Num(ws, lin, 15, f.CustoFreteEstimado);
                Num(ws, lin, 16, f.CustoTotalEstimado);

                Num(ws, lin, 17, f.CustoMaterialReal);
                Num(ws, lin, 18, f.CustoHorasReal);
                Num(ws, lin, 19, f.CustoPinturaReal);
                Num(ws, lin, 20, f.CustoFreteReal);
                Num(ws, lin, 21, f.CustoTotalReal);

                Num(ws, lin, 22, f.ValorVenda);
                Num(ws, lin, 23, f.LucroEstimado);
                Num(ws, lin, 24, f.LucroReal);
                Num(ws, lin, 25, f.DiffCusto);
                Num(ws, lin, 26, f.DiffLucro);
            }

            ws.Range(r, 1, lin, headers.Length).Style.Border.OutsideBorder =
                ws.Range(r, 1, lin, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.SheetView.FreezeRows(r);
            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 28;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static void Num(IXLWorksheet ws, int lin, int col, decimal v)
        {
            var cell = ws.Cell(lin, col);
            cell.Value = (double)v;
            cell.Style.NumberFormat.Format = "R$ #,##0.00";
        }

        /// <summary>
        /// Relatório das origens ("Nos conheceu por onde?") — clientes por origem.
        /// </summary>
        public static byte[] OrigensExcel(ICollection<OrigemResumoVM> itens, string titulo, DateTime geradoEm)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Origens");
            ws.Cell(1, 1).Value = titulo;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = "Gerado em: " + geradoEm.ToString("dd/MM/yyyy HH:mm");

            int r = 4;
            string[] headers = { "Origem", "Qtd. de Clientes", "Clientes" };
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(r, c + 1).Value = headers[c];
                ws.Cell(r, c + 1).Style.Font.Bold = true;
                ws.Cell(r, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
                ws.Cell(r, c + 1).Style.Font.FontColor = XLColor.White;
            }

            int lin = r;
            foreach (var it in itens)
            {
                lin++;
                ws.Cell(lin, 1).Value = it.NomeOrigem;
                ws.Cell(lin, 2).Value = it.Quantidade;
                ws.Cell(lin, 3).Value = string.Join(", ", it.Clientes);
            }

            ws.Range(r, 1, lin, headers.Length).Style.Border.OutsideBorder =
                ws.Range(r, 1, lin, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.SheetView.FreezeRows(r);
            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 26;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }

    /// <summary>Linha do relatório de projetos para exportação.</summary>
    public class ProjetoFinanceiroVM
    {
        public Projeto Projeto { get; set; } = new();
        public ProjetoFinanceiro Financeiro { get; set; } = new();
        public string GrupoNome { get; set; } = "-";
        public string NivelNome { get; set; } = "-";
        public List<string> Clientes { get; set; } = new();
    }

    /// <summary>Linha do relatório de origens para exportação.</summary>
    public class OrigemResumoVM
    {
        public string NomeOrigem { get; set; } = "-";
        public int Quantidade { get; set; }
        public List<string> Clientes { get; set; } = new();
    }
}
