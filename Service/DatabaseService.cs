using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DatabaseService
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Banco> _bancos;
    private readonly IMongoCollection<Conta> _contas;
    private readonly IMongoCollection<Categoria> _categorias;
    private readonly IMongoCollection<CustosFixos> _custosFixos;
    private readonly IMongoCollection<Lancamento> _lancamentos;
    private readonly IMongoCollection<Receita> _receitas;

    public DatabaseService(IMongoDatabase database)
    {
        _database = database;
        _bancos = database.GetCollection<Banco>("Banco");
        _contas = database.GetCollection<Conta>("Conta");
        _categorias = database.GetCollection<Categoria>("Categoria");
        _custosFixos = database.GetCollection<CustosFixos>("CustosFixos");
        _lancamentos = database.GetCollection<Lancamento>("Lancamento");
        _receitas = database.GetCollection<Receita>("Receita");
    }

    // 🔹 FUNÇÃO PRINCIPAL
    public async Task<ResultadoReset> LimparECriarTudo(bool manterConfiguracoes = false)
    {
        var resultado = new ResultadoReset
        {
            DataProcessamento = DateTime.Now,
            Inicio = DateTime.Now
        };

        try
        {
            await LimparBanco(manterConfiguracoes, resultado);
            await CriarDadosPadrao(resultado);
            await CriarDadosExemplo(resultado);

            resultado.Sucesso = true;
            resultado.Fim = DateTime.Now;
            resultado.TempoExecucao = resultado.Fim - resultado.Inicio;
            resultado.Mensagem = $"Banco resetado com sucesso! {resultado.TotalItens} itens criados.";
        }
        catch (Exception ex)
        {
            resultado.Sucesso = false;
            resultado.Mensagem = ex.Message;
            resultado.Erro = ex.ToString();
        }

        return resultado;
    }

    private async Task LimparBanco(bool manterConfiguracoes, ResultadoReset resultado)
    {
        if (!manterConfiguracoes)
        {
            await _bancos.DeleteManyAsync(_ => true);
            await _contas.DeleteManyAsync(_ => true);
            await _categorias.DeleteManyAsync(_ => true);
        }

        await _custosFixos.DeleteManyAsync(_ => true);
        await _lancamentos.DeleteManyAsync(_ => true);
        await _receitas.DeleteManyAsync(_ => true);
    }

    private async Task CriarDadosPadrao(ResultadoReset resultado)
    {
        var bancos = new List<Banco>
        {
            new Banco { CodigoBanco = "341", Nome = "Itaú" },
            new Banco { CodigoBanco = "260", Nome = "Nubank" }
        };

        await _bancos.InsertManyAsync(bancos);
        resultado.BancosCriados = bancos.Count;

        var categorias = new List<Categoria>
        {
            new Categoria { CodigoCategoria = "SALARIO", Nome = "Salário" },
            new Categoria { CodigoCategoria = "FREELA", Nome = "Freelance" },
            new Categoria { CodigoCategoria = "ALUGUEL", Nome = "Aluguel" },
            new Categoria { CodigoCategoria = "ENERGIA", Nome = "Energia" }
        };

        await _categorias.InsertManyAsync(categorias);
        resultado.CategoriasCriadas = categorias.Count;

        var contas = new List<Conta>
        {
            new Conta
            {
                Nome = "Conta Corrente",
                Tipo = TipoConta.Corrente,
                SaldoInicial = 3000,
                BancoId = bancos[0].Id!
            },
            new Conta
            {
                Nome = "Cartão Crédito",
                Tipo = TipoConta.Corrente,
                SaldoInicial = -1000,
                BancoId = bancos[0].Id!
            }
        };

        await _contas.InsertManyAsync(contas);
        resultado.ContasCriadas = contas.Count;
    }

    private async Task CriarDadosExemplo(ResultadoReset resultado)
    {
        var hoje = DateTime.Now;
        var categorias = await _categorias.Find(_ => true).ToListAsync();
        var contas = await _contas.Find(_ => true).ToListAsync();

        var contaCorrente = contas.First(c => c.SaldoInicial > 0);
        var cartaoCredito = contas.First(c => c.SaldoInicial < 0);

        // ===== CUSTOS FIXOS =====
        var custosFixos = new List<CustosFixos>
        {
            new CustosFixos
            {
                Descricao = "Aluguel",
                Valor = 1500,
                Vencimento = new DateTime(hoje.Year, hoje.Month, 5),
                CategoriaId = categorias.First(c => c.CodigoCategoria == "ALUGUEL").Id!
            }
        };

        await _custosFixos.InsertManyAsync(custosFixos);
        resultado.CustosFixosCriados = custosFixos.Count;

        var lancamentosCustos = custosFixos.Select(c => new Lancamento
        {
            Descricao = c.Descricao,
            Valor = c.Valor,
            Data = c.Vencimento,
            Tipo = TipoLancamento.Saida,
            Status = StatusPagamento.Pendente,
            CategoriaId = c.CategoriaId,
            ContaId = cartaoCredito.Id!
        }).ToList();

        await _lancamentos.InsertManyAsync(lancamentosCustos);
        resultado.LancamentosCriados += lancamentosCustos.Count;

        // ===== RECEITAS =====
        var receitas = new List<Receita>
        {
            new Receita
            {
                Descricao = "Salário",
                Valor = 4500,
                Data = new DateTime(hoje.Year, hoje.Month, 5),
                Status = StatusPagamento.Pago,
                CategoriaId = categorias.First(c => c.CodigoCategoria == "SALARIO").Id!,
                ContaId = contaCorrente.Id!
            }
        };

        await _receitas.InsertManyAsync(receitas);
        resultado.ReceitasCriadas = receitas.Count;

        var lancamentosReceitas = receitas.Select(r => new Lancamento
        {
            Descricao = r.Descricao,
            Valor = r.Valor,
            Data = r.Data,
            Tipo = TipoLancamento.Entrada,
            Status = r.Status,
            CategoriaId = r.CategoriaId,
            ContaId = r.ContaId
        }).ToList();

        await _lancamentos.InsertManyAsync(lancamentosReceitas);
        resultado.LancamentosCriados += lancamentosReceitas.Count;

        resultado.TotalItens =
            resultado.BancosCriados +
            resultado.CategoriasCriadas +
            resultado.ContasCriadas +
            resultado.CustosFixosCriados +
            resultado.ReceitasCriadas +
            resultado.LancamentosCriados;
    }
}


public class ResultadoReset
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = "";
    public string? Erro { get; set; }
    public DateTime DataProcessamento { get; set; }

    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public TimeSpan TempoExecucao { get; set; }

    public List<string> CollectionsLimpas { get; set; } = new();
    public int TotalLimpado { get; set; }

    public int BancosCriados { get; set; }
    public int CategoriasCriadas { get; set; }
    public int ContasCriadas { get; set; }
    public int CustosFixosCriados { get; set; }
    public int ReceitasCriadas { get; set; }
    public int LancamentosCriados { get; set; }

    public int TotalItens { get; set; }
    public List<string> ItensCriados { get; set; } = new();
}

public class ResultadoSimples
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = "";
    public string? Detalhes { get; set; }
}

public class StatusBanco
{
    public DateTime DataVerificacao { get; set; }

    public long Bancos { get; set; }
    public long Contas { get; set; }
    public long Categorias { get; set; }
    public long CustosFixos { get; set; }
    public long Receitas { get; set; }
    public long Lancamentos { get; set; }

    public long TotalItens { get; set; }
    public bool SistemaPronto { get; set; }
    public string Mensagem { get; set; } = "";
}

