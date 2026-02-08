using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class LancamentoService
{
    private readonly IMongoCollection<Lancamento> _lancamentos;
    private readonly IMongoCollection<CustosFixos> _custosFixos;
    private readonly IMongoCollection<Conta> _contas;
    private readonly IMongoCollection<Categoria> _categorias;

    public LancamentoService(IMongoDatabase database)
    {
        _lancamentos = database.GetCollection<Lancamento>("Lancamento");
        _custosFixos = database.GetCollection<CustosFixos>("CustosFixos");
        _contas = database.GetCollection<Conta>("Conta");
        _categorias = database.GetCollection<Categoria>("Categoria");
    }

    // 🔹 RELATÓRIO COMPLETO DE LANÇAMENTOS
    public async Task<RelatorioLancamentos> GerarRelatorio(DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var filtro = Builders<Lancamento>.Filter.Empty;
        
        if (dataInicio.HasValue || dataFim.HasValue)
        {
            var filtroData = Builders<Lancamento>.Filter.Empty;
            
            if (dataInicio.HasValue)
            {
                filtroData &= Builders<Lancamento>.Filter.Gte(x => x.Data, dataInicio.Value.Date);
            }
            
            if (dataFim.HasValue)
            {
                filtroData &= Builders<Lancamento>.Filter.Lte(x => x.Data, dataFim.Value.Date.AddDays(1).AddSeconds(-1));
            }
            
            filtro &= filtroData;
        }

        var lancamentos = await _lancamentos.Find(filtro).ToListAsync();
        
        // Buscar dados relacionados para enriquecer o relatório
        var contas = await _contas.Find(_ => true).ToListAsync();
        var categorias = await _categorias.Find(_ => true).ToListAsync();
        var custosFixos = await _custosFixos.Find(_ => true).ToListAsync();

        // Mapear IDs para nomes
        var contaDict = contas.ToDictionary(c => c.Id, c => c.Nome);
        var categoriaDict = categorias.ToDictionary(c => c.Id, c => c.Nome);

        // 🔹 CALCULAR VALORES
        var entradas = lancamentos
            .Where(x => x.Tipo == TipoLancamento.Entrada && x.Status == StatusPagamento.Pago)
            .Sum(x => x.Valor);

        var saidas = lancamentos
            .Where(x => x.Tipo == TipoLancamento.Saida && x.Status == StatusPagamento.Pago)
            .Sum(x => x.Valor);

        var pendentes = lancamentos
            .Where(x => x.Status == StatusPagamento.Pendente)
            .ToList();

        var receitasPendentes = pendentes
            .Where(x => x.Tipo == TipoLancamento.Entrada)
            .Sum(x => x.Valor);

        var despesasPendentes = pendentes
            .Where(x => x.Tipo == TipoLancamento.Saida)
            .Sum(x => x.Valor);

        var saldoAtual = entradas - saidas;
        var saldoProjetado = saldoAtual + receitasPendentes - despesasPendentes;

        // 🔹 IDENTIFICAR LANÇAMENTOS VENCIDOS
        var hoje = DateTime.Now.Date;
        var lancamentosVencidos = new List<LancamentoVencido>();
        var alertas = new List<string>();

        foreach (var lanc in pendentes.Where(x => x.Tipo == TipoLancamento.Saida))
        {
            var diasAtraso = (hoje - lanc.Data.Date).Days;
            
            if (diasAtraso > 0)
            {
                lancamentosVencidos.Add(new LancamentoVencido
                {
                    Lancamento = lanc,
                    DiasAtraso = diasAtraso,
                    Conta = contaDict.ContainsKey(lanc.ContaId) ? contaDict[lanc.ContaId] : "Não encontrada",
                    Categoria = categoriaDict.ContainsKey(lanc.CategoriaId) ? categoriaDict[lanc.CategoriaId] : "Não encontrada"
                });

                if (diasAtraso > 30)
                {
                    alertas.Add($"ALERTA: Lançamento '{lanc.Descricao}' está atrasado há {diasAtraso} dias!");
                }
            }
        }

        // 🔹 CUSTOS FIXOS VENCIDOS/PRÓXIMOS DO VENCIMENTO
        var custosVencidos = new List<CustoFixosVencido>();
        var custosProximos = new List<CustoFixosVencido>();

        foreach (var custo in custosFixos)
        {
            var diasParaVencimento = (custo.Vencimento.Date - hoje).Days;
            var categoria = categoriaDict.ContainsKey(custo.CategoriaId) ? categoriaDict[custo.CategoriaId] : "Não encontrada";

            var custoInfo = new CustoFixosVencido
            {
                CustoFixo = custo,
                DiasParaVencimento = diasParaVencimento,
                Categoria = categoria
            };

            if (diasParaVencimento < 0)
            {
                custosVencidos.Add(custoInfo);
                alertas.Add($"CUSTO FIXO VENCIDO: '{custo.Descricao}' de {categoria} está atrasado há {-diasParaVencimento} dias!");
            }
            else if (diasParaVencimento <= 7)
            {
                custosProximos.Add(custoInfo);
                if (diasParaVencimento <= 3)
                {
                    alertas.Add($"CUSTO FIXO PRÓXIMO: '{custo.Descricao}' vence em {diasParaVencimento} dias!");
                }
            }
        }

        // 🔹 RESUMO POR CATEGORIA
        var resumoCategorias = lancamentos
            .Where(x => x.Tipo == TipoLancamento.Saida && x.Status == StatusPagamento.Pago)
            .GroupBy(x => x.CategoriaId)
            .Select(g => new ResumoCategoria
            {
                CategoriaId = g.Key,
                Categoria = categoriaDict.ContainsKey(g.Key) ? categoriaDict[g.Key] : "Não encontrada",
                Total = g.Sum(x => x.Valor),
                Quantidade = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        // 🔹 RESUMO POR CONTA
        var resumoContas = lancamentos
            .GroupBy(x => x.ContaId)
            .Select(g => new ResumoConta
            {
                ContaId = g.Key,
                Conta = contaDict.ContainsKey(g.Key) ? contaDict[g.Key] : "Não encontrada",
                Entradas = g.Where(x => x.Tipo == TipoLancamento.Entrada && x.Status == StatusPagamento.Pago).Sum(x => x.Valor),
                Saidas = g.Where(x => x.Tipo == TipoLancamento.Saida && x.Status == StatusPagamento.Pago).Sum(x => x.Valor),
                Saldo = g.Where(x => x.Tipo == TipoLancamento.Entrada && x.Status == StatusPagamento.Pago).Sum(x => x.Valor) -
                        g.Where(x => x.Tipo == TipoLancamento.Saida && x.Status == StatusPagamento.Pago).Sum(x => x.Valor)
            })
            .OrderByDescending(x => x.Entradas - x.Saidas)
            .ToList();

        return new RelatorioLancamentos
        {
            Periodo = new PeriodoRelatorio 
            { 
                DataInicio = dataInicio ?? lancamentos.Min(x => x.Data), 
                DataFim = dataFim ?? lancamentos.Max(x => x.Data) 
            },
            Resumo = new ResumoFinanceiro
            {
                TotalEntradas = entradas,
                TotalSaidas = saidas,
                SaldoAtual = saldoAtual,
                ReceitasPendentes = receitasPendentes,
                DespesasPendentes = despesasPendentes,
                SaldoProjetado = saldoProjetado,
                TotalLancamentos = lancamentos.Count,
                LancamentosPendentes = pendentes.Count
            },
            LancamentosVencidos = lancamentosVencidos,
            CustosFixosVencidos = custosVencidos,
            CustosFixosProximos = custosProximos,
            ResumoPorCategoria = resumoCategorias,
            ResumoPorConta = resumoContas,
            Alertas = alertas,
            Lancamentos = lancamentos.Select(l => new LancamentoDetalhado
            {
                Lancamento = l,
                Conta = contaDict.ContainsKey(l.ContaId) ? contaDict[l.ContaId] : "Não encontrada",
                Categoria = categoriaDict.ContainsKey(l.CategoriaId) ? categoriaDict[l.CategoriaId] : "Não encontrada",
                StatusFormatado = l.Status.ToString(),
                TipoFormatado = l.Tipo == TipoLancamento.Entrada ? "Receita" : "Despesa",
                EstaVencido = l.Status == StatusPagamento.Pendente && 
                              l.Tipo == TipoLancamento.Saida && 
                              (hoje - l.Data.Date).Days > 0
            }).ToList()
        };
    }

    // 🔹 REGRAS DE ALERTA PERSONALIZADAS
    public async Task<List<RegraAlerta>> VerificarRegras()
    {
        var regras = new List<RegraAlerta>();
        var lancamentos = await _lancamentos.Find(_ => true).ToListAsync();
        var hoje = DateTime.Now.Date;

        // REGRA 1: Despesas pendentes com mais de 30 dias
        var despesasAtrasadas = lancamentos
            .Where(x => x.Status == StatusPagamento.Pendente && 
                       x.Tipo == TipoLancamento.Saida && 
                       (hoje - x.Data.Date).Days > 30)
            .ToList();

        if (despesasAtrasadas.Any())
        {
            regras.Add(new RegraAlerta
            {
                Tipo = TipoAlerta.Critico,
                Titulo = "Despesas Atrasadas",
                Mensagem = $"Existem {despesasAtrasadas.Count} despesas com mais de 30 dias de atraso",
                Detalhes = despesasAtrasadas.Select(d => $"- {d.Descricao}: R$ {d.Valor:F2} ({d.Data:dd/MM/yyyy})"),
                ValorTotal = despesasAtrasadas.Sum(d => d.Valor)
            });
        }

        // REGRA 2: Saldo negativo projetado
        var relatorio = await GerarRelatorio();
        if (relatorio.Resumo.SaldoProjetado < 0)
        {
            regras.Add(new RegraAlerta
            {
                Tipo = TipoAlerta.Alerta,
                Titulo = "Saldo Negativo Projetado",
                Mensagem = $"O saldo projetado está negativo: R$ {relatorio.Resumo.SaldoProjetado:F2}",
                Detalhes = new List<string> 
                { 
                    $"Saldo atual: R$ {relatorio.Resumo.SaldoAtual:F2}",
                    $"Despesas pendentes: R$ {relatorio.Resumo.DespesasPendentes:F2}"
                },
                ValorTotal = relatorio.Resumo.SaldoProjetado
            });
        }

        // REGRA 3: Alta concentração em uma categoria
        var resumoCategorias = relatorio.ResumoPorCategoria;
        if (resumoCategorias.Any())
        {
            var maiorCategoria = resumoCategorias.First();
            var totalDespesas = resumoCategorias.Sum(x => x.Total);
            var percentual = totalDespesas > 0 ? (maiorCategoria.Total / totalDespesas) * 100 : 0;

            if (percentual > 50)
            {
                regras.Add(new RegraAlerta
                {
                    Tipo = TipoAlerta.Informativo,
                    Titulo = "Concentração em Categoria",
                    Mensagem = $"A categoria '{maiorCategoria.Categoria}' representa {percentual:F1}% das despesas",
                    Detalhes = new List<string> 
                    { 
                        $"{maiorCategoria.Categoria}: R$ {maiorCategoria.Total:F2} ({percentual:F1}%)"
                    },
                    ValorTotal = maiorCategoria.Total
                });
            }
        }

        // REGRA 4: Custos fixos vencidos
        var custosFixos = await _custosFixos.Find(_ => true).ToListAsync();
        var custosVencidos = custosFixos.Where(c => c.Vencimento.Date < hoje).ToList();

        if (custosVencidos.Any())
        {
            regras.Add(new RegraAlerta
            {
                Tipo = TipoAlerta.Critico,
                Titulo = "Custos Fixos Vencidos",
                Mensagem = $"Existem {custosVencidos.Count} custos fixos vencidos",
                Detalhes = custosVencidos.Select(c => $"- {c.Descricao}: R$ {c.Valor:F2} (Venceu em {c.Vencimento:dd/MM/yyyy})"),
                ValorTotal = custosVencidos.Sum(c => c.Valor)
            });
        }

        return regras;
    }

    // 🔹 ATUALIZAR STATUS DE LANÇAMENTOS VENCIDOS
    public async Task<int> AtualizarStatusAutomatico()
    {
        var hoje = DateTime.Now.Date;
        var lancamentosPendentes = await _lancamentos
            .Find(x => x.Status == StatusPagamento.Pendente && x.Data.Date < hoje)
            .ToListAsync();

        if (!lancamentosPendentes.Any())
            return 0;

        // Você pode configurar regras diferentes aqui:
        // - Atualizar para "Pago" automaticamente
        // - Manter como "Pendente" mas marcar como atrasado
        // - Enviar para outra categoria

        // Exemplo: Marcar como atrasado (status continua pendente, mas podemos adicionar uma flag)
        var atualizados = 0;
        
        foreach (var lanc in lancamentosPendentes)
        {
            // Aqui você pode implementar lógicas específicas
            // Por exemplo, se a data de vencimento passou há mais de 60 dias, marcar como cancelado
            var diasAtraso = (hoje - lanc.Data.Date).Days;
            
            if (diasAtraso > 60)
            {
                lanc.Status = StatusPagamento.Cancelado;
                lanc.UpdatedAt = DateTime.Now;
                await _lancamentos.ReplaceOneAsync(x => x.Id == lanc.Id, lanc);
                atualizados++;
            }
        }

        return atualizados;
    }
}

// 🔹 MODELOS PARA O RELATÓRIO
public class RelatorioLancamentos
{
    public PeriodoRelatorio Periodo { get; set; }
    public ResumoFinanceiro Resumo { get; set; }
    public List<LancamentoVencido> LancamentosVencidos { get; set; } = new List<LancamentoVencido>();
    public List<CustoFixosVencido> CustosFixosVencidos { get; set; } = new List<CustoFixosVencido>();
    public List<CustoFixosVencido> CustosFixosProximos { get; set; } = new List<CustoFixosVencido>();
    public List<ResumoCategoria> ResumoPorCategoria { get; set; } = new List<ResumoCategoria>();
    public List<ResumoConta> ResumoPorConta { get; set; } = new List<ResumoConta>();
    public List<LancamentoDetalhado> Lancamentos { get; set; } = new List<LancamentoDetalhado>();
    public List<string> Alertas { get; set; } = new List<string>();
}

public class PeriodoRelatorio
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
}

public class ResumoFinanceiro
{
    public decimal TotalEntradas { get; set; }
    public decimal TotalSaidas { get; set; }
    public decimal SaldoAtual { get; set; }
    public decimal ReceitasPendentes { get; set; }
    public decimal DespesasPendentes { get; set; }
    public decimal SaldoProjetado { get; set; }
    public int TotalLancamentos { get; set; }
    public int LancamentosPendentes { get; set; }
}

public class LancamentoVencido
{
    public Lancamento Lancamento { get; set; }
    public int DiasAtraso { get; set; }
    public string Conta { get; set; }
    public string Categoria { get; set; }
}

public class CustoFixosVencido
{
    public CustosFixos CustoFixo { get; set; }
    public int DiasParaVencimento { get; set; }
    public string Categoria { get; set; }
}

public class ResumoCategoria
{
    public string CategoriaId { get; set; }
    public string Categoria { get; set; }
    public decimal Total { get; set; }
    public int Quantidade { get; set; }
    public decimal Percentual { get; set; }
}

public class ResumoConta
{
    public string ContaId { get; set; }
    public string Conta { get; set; }
    public decimal Entradas { get; set; }
    public decimal Saidas { get; set; }
    public decimal Saldo { get; set; }
}

public class LancamentoDetalhado
{
    public Lancamento Lancamento { get; set; }
    public string Conta { get; set; }
    public string Categoria { get; set; }
    public string StatusFormatado { get; set; }
    public string TipoFormatado { get; set; }
    public bool EstaVencido { get; set; }
}

public class RegraAlerta
{
    public TipoAlerta Tipo { get; set; }
    public string Titulo { get; set; }
    public string Mensagem { get; set; }
    public IEnumerable<string> Detalhes { get; set; }
    public decimal ValorTotal { get; set; }
}

public enum TipoAlerta
{
    Informativo,
    Alerta,
    Critico
}
