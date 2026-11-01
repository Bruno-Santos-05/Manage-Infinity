using System.Text;

namespace Infinity.Web.Helpers
{
    /// <summary>
    /// Gera arquivos PDF simples (relatórios tabulares) sem dependências externas.
    /// Suporte a múltiplas páginas, cabeçalho repetido, cores e acentuação (WinAnsi).
    /// </summary>
    public static class PdfReport
    {
        private const float PageWidth = 595f;   // A4
        private const float PageHeight = 842f;
        private const float Margin = 40f;
        private const float LineHeight = 16f;
        private const float ContentBottom = 60f;

        /// <summary>
        /// Cria um relatório em PDF.
        /// </summary>
        /// <param name="titulo">Título do relatório.</param>
        /// <param name="subtitulo">Subtítulo/texto de rodapé opcional.</param>
        /// <param name="colunas">Nomes das colunas.</param>
        /// <param name="linhas">Dados (cada item = uma linha, cada string = uma célula).</param>
        /// <param name="largurasRelativas">Larguras relativas das colunas (opcional; soma é normalizada).</param>
        public static byte[] Gerar(
            string titulo,
            string subtitulo,
            string[] colunas,
            IEnumerable<IEnumerable<string>> linhas,
            float[]? largurasRelativas = null)
        {
            var rows = linhas.Select(r => r.Select(c => c ?? "").ToArray()).ToList();

            // Resolve larguras das colunas
            int nCol = colunas.Length;
            if (largurasRelativas == null || largurasRelativas.Length != nCol)
                largurasRelativas = Enumerable.Repeat(1f, nCol).ToArray();
            float soma = largurasRelativas.Sum();
            var colW = largurasRelativas.Select(w => (PageWidth - 2 * Margin) * w / soma).ToArray();

            var buff = new List<byte>();
            long offset = 0;
            void Write(string s)
            {
                var b = Encoding.ASCII.GetBytes(s);
                buff.AddRange(b);
                offset += b.Length;
            }
            void WriteLine(string s) => Write(s + "\n");

            // Cabeçalho do arquivo PDF
            WriteLine("%PDF-1.4");

            // Para rastrear offsets dos objetos
            var objOffsets = new List<long>();

            // Nós vamos montar páginas. Cada página tem: objetos de página, fonts e conteúdo.
            // Fontes são compartilhadas (objetos fixos 3 e 4).
            const int fontObj = 3; // Helvetica
            const int fontBoldObj = 4; // Helvetica-Bold

            // --- Constrói o conteúdo (texto) dividido em páginas ---
            var pages = new List<List<string>>(); // comandos por página
            var current = new List<string>();
            float y = PageHeight - Margin - 30f; // posição inicial

            void AddPageBreak()
            {
                pages.Add(current);
                current = new List<string>();
                y = PageHeight - Margin - 20f;
            }

            // Título
            current.Add($"BT /F2 18 Tf {EscF(Margin)} {EscF(y)} Td ({Esc(titulo)}) Tj ET");
            y -= 22f;
            if (!string.IsNullOrWhiteSpace(subtitulo))
            {
                current.Add($"BT /F1 9 Tf {EscF(Margin)} {EscF(y)} Td ({Esc(subtitulo)}) Tj ET");
                y -= 18f;
            }
            y -= 8f;

            // Cabeçalho da tabela
            DrawRow(current, colunas, colW, y, true);
            y -= LineHeight;

            // Linhas
            foreach (var row in rows)
            {
                if (y < ContentBottom)
                    AddPageBreak();

                // Desenha fundo alternado
                current.Add($"0.96 0.96 0.96 rg {EscF(Margin)} {EscF(y - 3)} {EscF(PageWidth - 2 * Margin)} {EscF(LineHeight - 2)} re f");
                DrawRow(current, row, colW, y, false);
                y -= LineHeight;
            }

            pages.Add(current);

            // --- Escrita dos objetos ---
            // Objetos catalog + pages + fonts (compartilhados)
            objOffsets.Add(offset);
            WriteLine($"1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj");

            // children de pages = objetos de página começam em 5
            int firstPageObj = 5;
            var pageObjNumbers = Enumerable.Range(firstPageObj, pages.Count).ToArray();
            objOffsets.Add(offset);
            WriteLine($"2 0 obj\n<< /Type /Pages /Kids [{string.Join(" ", pageObjNumbers.Select(n => n + " 0 R"))}] /Count {pages.Count} >>\nendobj");

            objOffsets.Add(offset);
            WriteLine($"3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj");

            objOffsets.Add(offset);
            WriteLine($"4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj");

            // Agora os objectos de conteúdo das páginas
            int contentObjCounter = firstPageObj + pages.Count; // conteúdos depois das páginas
            int nextObj = contentObjCounter;

            // Precisamos saber os números dos conteúdos por página
            var contentType = new StringBuilder();
            for (int pi = 0; pi < pages.Count; pi++)
            {
                int pageObj = pageObjNumbers[pi];
                int contentObj = contentObjCounter + pi;

                // Objecto Page
                objOffsets.Add(offset);
                WriteLine($"{pageObj} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {EscF(PageWidth)} {EscF(PageHeight)}] " +
                          $"/Resources << /Font << /F1 {fontObj} 0 R /F2 {fontBoldObj} 0 R >> >> " +
                          $"/Contents {contentObj} 0 R >>\nendobj");

                // Conteúdo
                var stream = string.Join("\n", pages[pi]);
                var sBytes = Encoding.ASCII.GetBytes(stream + "\n");
                objOffsets.Add(offset);
                WriteLine($"{contentObj} 0 obj\n<< /Length {sBytes.Length} >>\nstream\n{stream}\nendstream\nendobj");
            }

            int count = nextObj - 1;

            // xref
            long xrefPos = offset;
            WriteLine("xref");
            int objCount = count; // objetos de 1..count
            WriteLine($"0 {objCount + 1}");
            WriteLine("0000000000 65535 f ");
            foreach (var o in objOffsets)
            {
                WriteLine($"{o:0000000000} 00000 n ");
            }

            WriteLine("trailer\n<< /Size " + (objCount + 1) + " /Root 1 0 R >>");
            WriteLine("startxref");
            WriteLine(xrefPos.ToString());
            WriteLine("%%EOF");

            return buff.ToArray();
        }

        private static void DrawRow(List<string> cmd, string[] values, float[] colW, float y, bool header)
        {
            // Fundo do cabeçalho
            if (header)
                cmd.Add($"0.17 0.24 0.31 rg {EscF(Margin)} {EscF(y - 3)} {EscF(PageWidth - 2 * Margin)} {EscF(LineHeight - 2)} re f");

            // Borda inferior sutil
            cmd.Add($"0.8 0.8 0.8 RG {EscF(Margin)} {EscF(y - 4)} {EscF(PageWidth - 2 * Margin)} m {EscF(PageWidth - Margin)} {EscF(y - 4)} l S");

            float x = Margin;
            for (int i = 0; i < values.Length && i < colW.Length; i++)
            {
                var texto = values[i];
                string font = header ? "/F2" : "/F1";
                // Trunca texto longo para caber na coluna
                int maxLen = (int)(colW[i] / 6.5f);
                if (texto.Length > maxLen) texto = texto.Substring(0, maxLen - 1) + "…";
                cmd.Add($"BT {font} 9 Tf {EscF(x + 3)} {EscF(y - 2)} Td ({Esc(texto)}) Tj ET");
                x += colW[i];
            }
        }

        private static string EscF(float v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        private static string Esc(string s)
        {
            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '(': sb.Append("\\("); break;
                    case ')': sb.Append("\\)"); break;
                    case '\\': sb.Append("\\\\"); break;
                    default:
                        int code = ch;
                        if (code < 127 && code >= 32)
                            sb.Append(ch);
                        else if (code < 256)
                            sb.Append((char)code); // WinAnsi/Latin-1
                        else
                            sb.Append('?');
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
