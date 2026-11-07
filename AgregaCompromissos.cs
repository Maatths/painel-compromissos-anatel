using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.FileIO;

// L\u00ea Compromissos_Investimento.csv (Anatel, dados abertos) direto do zip e gera dados.js compacto
public static class AgregaCompromissos
{
    class Dict
    {
        public List<string> L = new List<string>();
        Dictionary<string, int> m = new Dictionary<string, int>();
        public int Get(string s) { int i; if (!m.TryGetValue(s, out i)) { i = L.Count; L.Add(s); m[s] = i; } return i; }
    }

    static string SemAcento(string s)
    {
        var n = (s ?? "").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in n) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().ToLowerInvariant();
    }

    static string GrupoOperadora(string op)
    {
        var s = SemAcento(op);
        if (s.Contains("telefonica") || s.Contains("vivo")) return "Vivo (Telef\u00f4nica)";
        if (s.StartsWith("claro")) return "Claro";
        if (s.StartsWith("tim ") || s.StartsWith("tim/") || s == "tim") return "TIM";
        if (s.StartsWith("oi ") || s == "oi") return "Oi";
        if (s.Contains("brisanet")) return "Brisanet";
        if (s.Contains("algar")) return "Algar";
        if (s.Contains("sercomtel")) return "Sercomtel";
        return "Outras operadoras";
    }

    static string FamiliaOrigem(string o)
    {
        if (o.Contains("(Ed. do 5G)")) return "Edital 5G (2021)";
        if (o.Contains("700MHz")) return "Edital 700 MHz (2026)";
        if (o.Contains("(Ed. do 3G)")) return "Edital 3G (2007)";
        if (o.Contains("(Ed. do 4G)")) return "Edital 4G (2012)";
        if (o.Contains("Banda H")) return "Edital Banda H (2010)";
        if (o.Contains("Sobras")) return "Edital Sobras do SMP (2007)";
        if (o.StartsWith("FUST")) return "FUST (MCOM/BNDES)";
        if (o == "TAC") return "TAC";
        if (o.StartsWith("PGMU")) return "PGMU";
        if (o.StartsWith("GIRED") || o.StartsWith("GAISPI") || o == "ODF" || o.StartsWith("Termo de Concilia")) return "Outros programas";
        if (o.StartsWith("Termo de Autocomp")) return "Termo de Autocomposi\u00e7\u00e3o";
        return o;
    }

    static string J(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ") + "\""; }
    static string JL(IEnumerable<string> l) { return "[" + string.Join(",", l.Select(J)) + "]"; }

    public static string Run(string zipPath, string outPath) { return Run(zipPath, outPath, null); }

    // grupo: se informado (ex.: "Vivo (Telefônica)"), mantém só as linhas desse grupo econômico
    public static string Run(string zipPath, string outPath, string grupo)
    {
        var op = new Dict(); var orig = new Dict(); var tipo = new Dict(); var nivel = new Dict();
        var tec = new Dict(); var st = new Dict(); var comp = new Dict();
        var munIdx = new Dictionary<string, int>(); var mun = new List<string[]>();
        var origensBrutas = new SortedDictionary<string, int>(); var opsOutras = new Dictionary<string, int>();
        var rows = new StringBuilder(); int n = 0;
        var d0 = new DateTime(2000, 1, 1);

        using (var zip = ZipFile.OpenRead(zipPath))
        {
            var e = zip.Entries.First(x => x.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
            using (var p = new TextFieldParser(e.Open(), Encoding.UTF8))
            {
                p.SetDelimiters(";"); p.HasFieldsEnclosedInQuotes = true;
                var h = p.ReadFields(); var ix = new Dictionary<string, int>();
                for (int i = 0; i < h.Length; i++) ix[h[i].Trim()] = i;
                Func<string[], string, string> F = (f, c) => (f[ix[c]] ?? "").Trim();
                while (!p.EndOfData)
                {
                    var f = p.ReadFields(); if (f.Length < h.Length) continue;
                    if (!string.IsNullOrEmpty(grupo) && GrupoOperadora(F(f, "Operadora")) != grupo) continue;
                    string cod = F(f, "Cod_Municipio");
                    int mi;
                    if (!munIdx.TryGetValue(cod, out mi)) { mi = mun.Count; munIdx[cod] = mi; mun.Add(new[] { cod, F(f, "Municipio"), F(f, "UF"), F(f, "Regiao") }); }
                    string o = F(f, "Origem"); int oc; origensBrutas.TryGetValue(o, out oc); origensBrutas[o] = oc + 1;
                    string g = GrupoOperadora(F(f, "Operadora"));
                    if (g == "Outras operadoras") { int c2; opsOutras.TryGetValue(F(f, "Operadora"), out c2); opsOutras[F(f, "Operadora")] = c2 + 1; }
                    string c = F(f, "Compromisso");
                    if (c == "" || c == "N/D" || c == "N/A") c = F(f, "Tipo_Compromisso") + " (" + F(f, "Nivel_Obrigacao") + ")";
                    string tc = F(f, "Tecnologia_SMP"); if (tc == "N/A" || tc == "N/D" || tc == "") tc = "N\u00e3o se aplica";
                    DateTime dt; int dias = -1;
                    if (DateTime.TryParseExact(F(f, "Prazo_Maximo"), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) dias = (int)(dt - d0).TotalDays;
                    if (n > 0) rows.Append(',');
                    rows.Append('[').Append(mi).Append(',').Append(op.Get(g)).Append(',').Append(orig.Get(FamiliaOrigem(o))).Append(',')
                        .Append(tipo.Get(F(f, "Tipo_Compromisso"))).Append(',').Append(nivel.Get(F(f, "Nivel_Obrigacao"))).Append(',')
                        .Append(tec.Get(tc)).Append(',').Append(st.Get(F(f, "Status"))).Append(',').Append(comp.Get(c)).Append(',').Append(dias).Append(']');
                    n++;
                }
            }
        }

        var sb = new StringBuilder();
        sb.Append("// Gerado a partir de Anatel Dados Abertos: paineis_de_dados/acompanhamento_e_controle/compromissos_investimento.zip\n");
        sb.Append("// Linha: [municipio, operadora, origem, tipo, nivel, tecnologia, status, compromisso, prazo (dias desde 2000-01-01; -1 = sem prazo)]\n");
        sb.Append("window.DADOS = {\n");
        sb.Append("\"geradoEm\":" + J(DateTime.Now.ToString("yyyy-MM-dd")) + ",\n");
        sb.Append("\"mun\":[" + string.Join(",", mun.Select(m => JL(m))) + "],\n");
        sb.Append("\"op\":" + JL(op.L) + ",\n\"orig\":" + JL(orig.L) + ",\n\"tipo\":" + JL(tipo.L) + ",\n\"nivel\":" + JL(nivel.L) + ",\n");
        sb.Append("\"tec\":" + JL(tec.L) + ",\n\"st\":" + JL(st.L) + ",\n\"comp\":" + JL(comp.L) + ",\n");
        sb.Append("\"rows\":[" + rows + "]\n};\n");
        File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));

        return "linhas=" + n + " municipios=" + mun.Count + " kb=" + (new FileInfo(outPath).Length / 1024)
            + "\nop=" + string.Join(" | ", op.L) + "\norig=" + string.Join(" | ", orig.L)
            + "\nst=" + string.Join(" | ", st.L) + "\ntec=" + string.Join(" | ", tec.L) + "\nnivel=" + string.Join(" | ", nivel.L)
            + "\norigens brutas=" + string.Join(" | ", origensBrutas.Select(kv => kv.Key + "=" + kv.Value))
            + "\ntop outras=" + string.Join(" | ", opsOutras.OrderByDescending(kv => kv.Value).Take(8).Select(kv => kv.Key + "=" + kv.Value));
    }
}
