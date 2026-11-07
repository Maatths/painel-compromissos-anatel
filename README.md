# 🗺️ Compromissos Regulatórios · Telefônica Vivo

Painel web interativo para acompanhar as **obrigações de cobertura e infraestrutura da Telefônica Brasil (Vivo)** em editais de radiofrequência, termos de ajustamento e programas públicos acompanhados pela **Anatel**, por município, com prazos e status.

É uma **versão demonstrativa, só com dados públicos,** de um painel de acompanhamento regulatório que desenvolvi profissionalmente. O visual segue a paleta do painel original (roxo e rosa), sem logotipo. Não é um produto oficial da Vivo, da Telefônica Brasil nem da Anatel.

**[▶ Abrir o painel](https://maatths.github.io/painel-compromissos-anatel/)**

---

## 🎯 Para que serve

Quem acompanha obrigações regulatórias precisa responder rápido:

- O que **vence nos próximos 12 meses** e onde?
- Qual a **situação de cada edital** (3G, 4G, 5G, 700 MHz, TAC, FUST…)?
- Quais compromissos um **município específico** tem, de quais operadoras e com qual prazo?
- Onde os compromissos se concentram (UF, tipo, tecnologia exigida)?

## ✨ Funcionalidades

- **Filtros cruzados:** município (nome ou código IBGE), operadora, origem, tipo, tecnologia, status e UF. Os gráficos também filtram ao clicar.
- **Status recalculado pela data de hoje:** um compromisso "A vencer" com prazo passado vira "Vencido" automaticamente.
- **KPIs e frase-resumo** automáticos para o recorte selecionado.
- **Situação por origem:** barras 100% empilhadas por status.
- **Prazos por ano:** colunas empilhadas por status, com destaque para o ano atual.
- **Mapa do Brasil por UF** (faixas por quantil), operadoras, tipos e tecnologias.
- **Ficha por município:** todos os compromissos, agrupados e ordenados por urgência.
- Cada gráfico tem **visão em tabela**, tema claro e escuro e layout para celular. O status sempre aparece com ícone e rótulo, não só pela cor.

## 🗂️ Dados

| Item | Detalhe |
|---|---|
| Fonte | [Anatel: Dados Abertos](https://www.gov.br/anatel/pt-br/dados/dados-abertos), Painel de Acompanhamento e Controle |
| Arquivo | [`compromissos_investimento.zip`](https://www.anatel.gov.br/dadosabertos/paineis_de_dados/acompanhamento_e_controle/compromissos_investimento.zip) → `Compromissos_Investimento.csv` |
| Volume no painel | 21.014 compromissos da Telefônica Vivo em 4.313 municípios (a base completa tem 64.460, de 89 operadoras) |

**Tratamento** ([`AgregaCompromissos.cs`](AgregaCompromissos.cs)): o agregador lê o CSV direto do zip e trata os campos com quebra de linha entre aspas. Ele padroniza as operadoras por grupo econômico (as várias grafias de "Telefônica Brasil S.A." viram um grupo só), mantém só o grupo escolhido, agrupa as origens por edital ou programa, converte os prazos e gera o `dados.js` compacto (~0,7 MB), com dicionários e linhas em vetores de inteiros. Sem o terceiro parâmetro, ele gera a base de todas as operadoras.

## 🛠️ Tecnologias

`JavaScript` (sem frameworks) · `HTML/CSS` · `C#` (ETL)

## ▶️ Como atualizar

1. Baixe a versão mais recente de `compromissos_investimento.zip` (link acima).
2. Rode o agregador para gerar um novo `dados.js`. No PowerShell:
   ```powershell
   Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem, Microsoft.VisualBasic
   Add-Type -Path .\AgregaCompromissos.cs -ReferencedAssemblies System.IO.Compression, System.IO.Compression.FileSystem, System.Core, Microsoft.VisualBasic
   [AgregaCompromissos]::Run("C:\caminho\compromissos_investimento.zip", "$PWD\dados.js", "Vivo (Telefônica)")
   ```
3. Abra o `index.html`.

## ⚖️ Observações

- Projeto independente, só com dados abertos da Anatel e nenhuma informação interna. Não é um produto oficial da Vivo, da Telefônica Brasil nem da Anatel.
- O status vem da base da Anatel. Só a regra "Vencido" é calculada pelo painel.

---

👤 **Matheus Oliveira**: [LinkedIn](https://www.linkedin.com/in/matheus-oliveira-06b0b5162) · [GitHub](https://github.com/Maatths)
