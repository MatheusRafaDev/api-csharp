
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CalculadoraService
{
    private readonly IMongoCollection<Lancamento> _lancamentos;
    private readonly IMongoCollection<CustosFixos> _custosFixos;
    private readonly IMongoCollection<Conta> _contas;
    private readonly IMongoCollection<Categoria> _categorias;

    public CalculadoraService(IMongoDatabase database)
    {
        _lancamentos = database.GetCollection<Lancamento>("Lancamento");
        _custosFixos = database.GetCollection<CustosFixos>("CustosFixos");
        _contas = database.GetCollection<Conta>("Conta");
        _categorias = database.GetCollection<Categoria>("Categoria");
    }

    // 🔹 FUNÇÃO PRINCIPAL: CALCULAR TUDO DE UMA VEZ
    public async Task<ResultadoCalculos> CalcularTudo(DateTime? dataReferencia = null)
    {
        var resultado = new ResultadoCalculos
        {
            DataCalculo = DateTime.Now,
            DataReferencia = dataReferencia ?? DateTime.Now
        };

        try
        {
            // 1. BUSCAR TODOS OS DADOS
            var todosLancamentos = await _lancamentos.Find(_ => true).ToListAsync();
            var todosCustosFixos = await _custosFixos.Find(_ => true).ToListAsync();
            var todasContas = await _contas.Find(_ => true).ToListAsync();
            var todasCategorias = await _categorias.Find(_ => true).ToListAsync();

            // 2. CALCULAR SALDOS
            resultado = await CalcularSaldos(todosLancamentos, resultado);

            // 3. IDENTIFICAR VENCIDOS
            resultado = await IdentificarVencidos(todosLancamentos, todosCustosFixos, resultado);

            // 4. RESUMO POR CATEGORIA
            resultado = await ResumoPorCategoria(todosLancamentos, todasCategorias, resultado);

            // 5. RESUMO POR CONTA
            resultado = await ResumoPorConta(todosLancamentos, todasContas, resultado);

            // 6. CALCULAR PROJEÇÕES
            resultado = await CalcularProjecoes(todosLancamentos, resultado);

            resultado.Sucesso = true;
            resultado.Mensagem = "Cálculos realizados com sucesso";
        }
        catch (Exception ex)
        {
            resultado.Sucesso = false;
            resultado.Mensagem = $"Erro nos cálculos: {ex.Message}";
        }

        return resultado;
    }

    // 🔹 CALCULAR SALDOS
    private async Task<ResultadoCalculos> CalcularSaldos(List<Lancamento> lancamentos, ResultadoCalculos resultado)
    {
        var hoje = DateTime.Now;

        // Lançamentos já PAGOS
        var lancamentosPagos = lancamentos.Where(l => l.Status == StatusPagamento.Pago).ToList();
        
        resultado.TotalEntradas = lancamentosPagos
            .Where(l => l.Tipo == TipoLancamento.Entrada)
            .Sum(l => l.Valor);

        resultado.TotalSaidas = lancamentosPagos
            .Where(l => l.Tipo == TipoLancamento.Saida)
            .Sum(l => l.Valor);

        resultado.SaldoAtual = resultado.TotalEntradas - resultado.TotalSaidas;

        // Lançamentos PENDENTES
        var lancamentosPendentes = lancamentos.Where(l => l.Status == StatusPagamento.Pendente).ToList();
        
        resultado.ReceitasPendentes = lancamentosPendentes
            .Where(l => l.Tipo == TipoLancamento.Entrada)
            .Sum(l => l.Valor);

        resultado.DespesasPendentes = lancamentosPendentes
            .Where(l => l.Tipo == TipoLancamento.Saida)
            .Sum(l => l.Valor);

        resultado.SaldoProjetado = resultado.SaldoAtual + 
                                   resultado.ReceitasPendentes - 
                                   resultado.DespesasPendentes;

        // Contagem
        resultado.TotalLancamentos = lancamentos.Count;
        resultado.LancamentosPagos = lancamentosPagos.Count;
        resultado.LancamentosPendentes = lancamentosPendentes.Count;

        return resultado;
    }

    // 🔹 IDENTIFICAR VENCIDOS
    private async Task<ResultadoCalculos> IdentificarVencidos(
        List<Lancamento> lancamentos, 
        List<CustosFixos> custosFixos, 
        ResultadoCalculos resultado)
    {
        var hoje = DateTime.Now.Date;

        // 1. Lançamentos vencidos (pendentes com data passada)
        var lancamentosVencidos = lancamentos
            .Where(l => l.Status == StatusPagamento.Pendente &&
                       l.Tipo == TipoLancamento.Saida &&
                       l.Data.Date < hoje)
            .Select(l => new ItemVencido
            {
                Id = l.Id!,
                Descricao = l.Descricao,
                Valor = l.Valor,
                DataVencimento = l.Data,
                DiasAtraso = (hoje - l.Data.Date).Days,
                Tipo = "Lançamento",
                Prioridade = (hoje - l.Data.Date).Days > 30 ? "ALTA" : "MÉDIA"
            })
            .ToList();

        // 2. Custos fixos vencidos
        var custosVencidos = custosFixos
            .Where(c => c.Vencimento.Date < hoje)
            .Select(c => new ItemVencido
            {
                Id = c.Id!,
                Descricao = c.Descricao,
                Valor = c.Valor,
                DataVencimento = c.Vencimento,
                DiasAtraso = (hoje - c.Vencimento.Date).Days,
                Tipo = "Custo Fixo",
                Prioridade = (hoje - c.Vencimento.Date).Days > 30 ? "ALTA" : "MÉDIA"
            })
            .ToList();

        // 3. Próximos vencimentos (até 7 dias)
        var proximosVencimentos = lancamentos
            .Where(l => l.Status == StatusPagamento.Pendente &&
                       l.Tipo == TipoLancamento.Saida &&
                       l.Data.Date >= hoje &&
                       l.Data.Date <= hoje.AddDays(7))
            .Select(l => new ItemVencido
            {
                Id = l.Id!,
                Descricao = l.Descricao,
                Valor = l.Valor,
                DataVencimento = l.Data,
                DiasParaVencimento = (l.Data.Date - hoje).Days,
                Tipo = "Lançamento",
                Prioridade = l.Data.Date == hoje ? "HOJE" : "PRÓXIMO"
            })
            .ToList();

        resultado.LancamentosVencidos = lancamentosVencidos;
        resultado.CustosFixosVencidos = custosVencidos;
        resultado.ProximosVencimentos = proximosVencimentos;
        resultado.TotalVencidos = lancamentosVencidos.Count + custosVencidos.Count;
        resultado.ValorTotalVencido = lancamentosVencidos.Sum(l => l.Valor) + 
                                      custosVencidos.Sum(c => c.Valor);

        return resultado;
    }

    // 🔹 RESUMO POR CATEGORIA
    private async Task<ResultadoCalculos> ResumoPorCategoria(
        List<Lancamento> lancamentos, 
        List<Categoria> categorias, 
        ResultadoCalculos resultado)
    {
        var resumo = lancamentos
            .Where(l => l.Status == StatusPagamento.Pago && l.Tipo == TipoLancamento.Saida)
            .GroupJoin(categorias,
                lancamento => lancamento.CategoriaId,
                categoria => categoria.Id,
                (lancamento, categoriasList) => new
                {
                    Lancamento = lancamento,
                    Categoria = categoriasList.FirstOrDefault()
                })
            .GroupBy(x => x.Categoria?.Nome ?? "Sem Categoria")
            .Select(g => new ResumoCategoria
            {
                Categoria = g.Key,
                Total = g.Sum(x => x.Lancamento.Valor),
                Quantidade = g.Count(),
                Percentual = resultado.TotalSaidas > 0 ? 
                    (g.Sum(x => x.Lancamento.Valor) / resultado.TotalSaidas) * 100 : 0
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        resultado.ResumoCategorias = resumo;
        resultado.CategoriaMaiorGasto = resumo.FirstOrDefault()?.Categoria ?? "Nenhuma";
        resultado.ValorMaiorGasto = resumo.FirstOrDefault()?.Total ?? 0;

        return resultado;
    }

    // 🔹 RESUMO POR CONTA
    private async Task<ResultadoCalculos> ResumoPorConta(
        List<Lancamento> lancamentos, 
        List<Conta> contas, 
        ResultadoCalculos resultado)
    {
        var resumo = lancamentos
            .GroupJoin(contas,
                lancamento => lancamento.ContaId,
                conta => conta.Id,
                (lancamento, contasList) => new
                {
                    Lancamento = lancamento,
                    Conta = contasList.FirstOrDefault()
                })
            .GroupBy(x => x.Conta?.Nome ?? "Sem Conta")
            .Select(g => new ResumoConta
            {
                Conta = g.Key,
                Entradas = g.Where(x => x.Lancamento.Tipo == TipoLancamento.Entrada && 
                                       x.Lancamento.Status == StatusPagamento.Pago)
                           .Sum(x => x.Lancamento.Valor),
                Saidas = g.Where(x => x.Lancamento.Tipo == TipoLancamento.Saida && 
                                     x.Lancamento.Status == StatusPagamento.Pago)
                         .Sum(x => x.Lancamento.Valor),
                Saldo = g.Where(x => x.Lancamento.Tipo == TipoLancamento.Entrada && 
                                    x.Lancamento.Status == StatusPagamento.Pago)
                        .Sum(x => x.Lancamento.Valor) -
                       g.Where(x => x.Lancamento.Tipo == TipoLancamento.Saida && 
                                   x.Lancamento.Status == StatusPagamento.Pago)
                        .Sum(x => x.Lancamento.Valor)
            })
            .OrderByDescending(r => r.Saldo)
            .ToList();

        resultado.ResumoContas = resumo;
        resultado.ContaMaiorSaldo = resumo.FirstOrDefault()?.Conta ?? "Nenhuma";
        resultado.ValorMaiorSaldo = resumo.FirstOrDefault()?.Saldo ?? 0;

        return resultado;
    }

    // 🔹 CALCULAR PROJEÇÕES
    private async Task<ResultadoCalculos> CalcularProjecoes(List<Lancamento> lancamentos, ResultadoCalculos resultado)
    {
        var hoje = DateTime.Now;
        var fimDoMes = new DateTime(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month));

        // Despesas pendentes que vencem este mês
        var despesasMes = lancamentos
            .Where(l => l.Status == StatusPagamento.Pendente &&
                       l.Tipo == TipoLancamento.Saida &&
                       l.Data.Date >= hoje.Date &&
                       l.Data.Date <= fimDoMes)
            .Sum(l => l.Valor);

        // Receitas pendentes que vencem este mês
        var receitasMes = lancamentos
            .Where(l => l.Status == StatusPagamento.Pendente &&
                       l.Tipo == TipoLancamento.Entrada &&
                       l.Data.Date >= hoje.Date &&
                       l.Data.Date <= fimDoMes)
            .Sum(l => l.Valor);

        resultado.ProjecaoMes = new ProjecaoMes
        {
            DataInicio = hoje,
            DataFim = fimDoMes,
            DespesasRestantes = despesasMes,
            ReceitasRestantes = receitasMes,
            SaldoProjetadoFimMes = resultado.SaldoAtual + receitasMes - despesasMes,
            DiasRestantes = (fimDoMes - hoje.Date).Days
        };

        // Alertas
        var alertas = new List<string>();
        
        if (resultado.SaldoProjetado < 0)
            alertas.Add($"⚠️ ALERTA: Saldo projetado negativo: R$ {resultado.SaldoProjetado:F2}");
        
        if (resultado.TotalVencidos > 0)
            alertas.Add($"⚠️ ALERTA: {resultado.TotalVencidos} itens vencidos totalizando R$ {resultado.ValorTotalVencido:F2}");
        
        if (resultado.DespesasPendentes > resultado.ReceitasPendentes * 2)
            alertas.Add($"⚠️ ALERTA: Despesas pendentes são mais que o dobro das receitas pendentes");
        
        if (resultado.ProjecaoMes.SaldoProjetadoFimMes < 0)
            alertas.Add($"⚠️ ALERTA: Projeção de saldo negativo no fim do mês: R$ {resultado.ProjecaoMes.SaldoProjetadoFimMes:F2}");

        resultado.Alertas = alertas;

        return resultado;
    }

    // 🔹 FUNÇÃO SIMPLES: APENAS SALDO ATUAL
    public async Task<decimal> CalcularSaldoAtual()
    {
        var lancamentos = await _lancamentos.Find(_ => true).ToListAsync();
        
        var entradas = lancamentos
            .Where(l => l.Status == StatusPagamento.Pago && l.Tipo == TipoLancamento.Entrada)
            .Sum(l => l.Valor);

        var saidas = lancamentos
            .Where(l => l.Status == StatusPagamento.Pago && l.Tipo == TipoLancamento.Saida)
            .Sum(l => l.Valor);

        return entradas - saidas;
    }

    // 🔹 FUNÇÃO SIMPLES: TOTAL VENCIDO
    public async Task<decimal> CalcularTotalVencido()
    {
        var hoje = DateTime.Now.Date;
        var lancamentos = await _lancamentos.Find(_ => true).ToListAsync();
        
        var vencidos = lancamentos
            .Where(l => l.Status == StatusPagamento.Pendente &&
                       l.Tipo == TipoLancamento.Saida &&
                       l.Data.Date < hoje)
            .Sum(l => l.Valor);

        return vencidos;
    }

    // 🔹 FUNÇÃO SIMPLES: RESUMO RÁPIDO
    public async Task<object> ResumoRapido()
    {
        var resultado = await CalcularTudo();
        
        return new
        {
            resultado.SaldoAtual,
            resultado.SaldoProjetado,
            resultado.TotalVencidos,
            resultado.ValorTotalVencido,
            Alertas = resultado.Alertas.Take(3).ToList(),
            CategoriaMaiorGasto = resultado.ResumoCategorias.FirstOrDefault()?.Categoria ?? "Nenhuma"
        };
    }
}

// 🔹 MODELOS PARA OS CÁLCULOS
public class ResultadoCalculos
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = "";
    public DateTime DataCalculo { get; set; }
    public DateTime DataReferencia { get; set; }

    // Saldos
    public decimal TotalEntradas { get; set; }
    public decimal TotalSaidas { get; set; }
    public decimal SaldoAtual { get; set; }
    public decimal ReceitasPendentes { get; set; }
    public decimal DespesasPendentes { get; set; }
    public decimal SaldoProjetado { get; set; }

    // Contagens
    public int TotalLancamentos { get; set; }
    public int LancamentosPagos { get; set; }
    public int LancamentosPendentes { get; set; }

    // Vencidos
    public List<ItemVencido> LancamentosVencidos { get; set; } = new List<ItemVencido>();
    public List<ItemVencido> CustosFixosVencidos { get; set; } = new List<ItemVencido>();
    public List<ItemVencido> ProximosVencimentos { get; set; } = new List<ItemVencido>();
    public int TotalVencidos { get; set; }
    public decimal ValorTotalVencido { get; set; }

    // Resumos
    public List<ResumoCategoria> ResumoCategorias { get; set; } = new List<ResumoCategoria>();
    public List<ResumoConta> ResumoContas { get; set; } = new List<ResumoConta>();
    public string CategoriaMaiorGasto { get; set; } = "";
    public decimal ValorMaiorGasto { get; set; }
    public string ContaMaiorSaldo { get; set; } = "";
    public decimal ValorMaiorSaldo { get; set; }

    // Projeções
    public ProjecaoMes ProjecaoMes { get; set; } = new ProjecaoMes();

    // Alertas
    public List<string> Alertas { get; set; } = new List<string>();
}

public class ItemVencido
{
    public string Id { get; set; } = "";
    public string Descricao { get; set; } = "";
    public decimal Valor { get; set; }
    public DateTime DataVencimento { get; set; }
    public int DiasAtraso { get; set; }
    public int DiasParaVencimento { get; set; }
    public string Tipo { get; set; } = "";
    public string Prioridade { get; set; } = "";
}


public class ProjecaoMes
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public decimal DespesasRestantes { get; set; }
    public decimal ReceitasRestantes { get; set; }
    public decimal SaldoProjetadoFimMes { get; set; }
    public int DiasRestantes { get; set; }
}
