
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

    // 🔹 FUNÇÃO PRINCIPAL: LIMPAR E RECRIAR TUDO
    public async Task<ResultadoReset> LimparECriarTudo(bool manterConfiguracoes = false)
    {
        var resultado = new ResultadoReset
        {
            DataProcessamento = DateTime.Now,
            Inicio = DateTime.Now
        };

        try
        {
            // 1. LIMPAR BANCO
            await LimparBanco(manterConfiguracoes, resultado);

            // 2. CRIAR DADOS PADRÃO
            await CriarDadosPadrao(resultado);

            // 3. CRIAR DADOS DE EXEMPLO
            await CriarDadosExemplo(resultado);

            resultado.Sucesso = true;
            resultado.Mensagem = $"Banco resetado com sucesso! {resultado.TotalItens} itens criados.";
            resultado.Fim = DateTime.Now;
            resultado.TempoExecucao = resultado.Fim - resultado.Inicio;
        }
        catch (Exception ex)
        {
            resultado.Sucesso = false;
            resultado.Mensagem = $"Erro ao resetar banco: {ex.Message}";
            resultado.Erro = ex.ToString();
        }

        return resultado;
    }

    // 🔹 LIMPAR BANCO DE DADOS
    private async Task LimparBanco(bool manterConfiguracoes, ResultadoReset resultado)
    {
        if (!manterConfiguracoes)
        {
            // Limpar TUDO
            await _bancos.DeleteManyAsync(_ => true);
            await _contas.DeleteManyAsync(_ => true);
            await _categorias.DeleteManyAsync(_ => true);
            resultado.CollectionsLimpas.AddRange(new[] { "Bancos", "Contas", "Categorias" });
        }

        // Sempre limpar dados transacionais
        await _custosFixos.DeleteManyAsync(_ => true);
        await _lancamentos.DeleteManyAsync(_ => true);
        await _receitas.DeleteManyAsync(_ => true);
        resultado.CollectionsLimpas.AddRange(new[] { "CustosFixos", "Lancamentos", "Receitas" });

        resultado.TotalLimpado = resultado.CollectionsLimpas.Count;
    }

    // 🔹 CRIAR DADOS PADRÃO (ESSENCIAIS)
    private async Task CriarDadosPadrao(ResultadoReset resultado)
    {
        // 1. BANCOS PADRÃO DO BRASIL
        var bancosPadrao = new List<Banco>
        {
            new Banco { CodigoBanco = "001", Nome = "Banco do Brasil", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Banco { CodigoBanco = "033", Nome = "Santander", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Banco { CodigoBanco = "104", Nome = "Caixa Econômica Federal", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Banco { CodigoBanco = "237", Nome = "Bradesco", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Banco { CodigoBanco = "341", Nome = "Itaú", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Banco { CodigoBanco = "260", Nome = "Nubank", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Banco { CodigoBanco = "077", Nome = "Inter", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
        };

        await _bancos.InsertManyAsync(bancosPadrao);
        resultado.BancosCriados = bancosPadrao.Count;
        resultado.ItensCriados.AddRange(bancosPadrao.Select(b => $"Banco: {b.Nome}"));

        // 2. CATEGORIAS ESSENCIAIS
        var categoriasPadrao = new List<Categoria>
        {
            // RECEITAS
            new Categoria { CodigoCategoria = "SALARIO", Nome = "Salário", Descricao = "Recebimento de salário" },
            new Categoria { CodigoCategoria = "FREELA", Nome = "Freelance", Descricao = "Trabalhos autônomos" },
            new Categoria { CodigoCategoria = "INVEST", Nome = "Investimentos", Descricao = "Rendimentos de investimentos" },
            new Categoria { CodigoCategoria = "BONUS", Nome = "Bônus", Descricao = "Bônus e comissões" },
            
            // DESPESAS FIXAS
            new Categoria { CodigoCategoria = "ALUGUEL", Nome = "Aluguel", Descricao = "Pagamento de aluguel" },
            new Categoria { CodigoCategoria = "CONDOM", Nome = "Condomínio", Descricao = "Taxa de condomínio" },
            new Categoria { CodigoCategoria = "ENERGIA", Nome = "Energia Elétrica", Descricao = "Conta de luz" },
            new Categoria { CodigoCategoria = "AGUA", Nome = "Água", Descricao = "Conta de água" },
            new Categoria { CodigoCategoria = "GAS", Nome = "Gás", Descricao = "Botijão de gás" },
            new Categoria { CodigoCategoria = "INTERNET", Nome = "Internet", Descricao = "Provedor de internet" },
            new Categoria { CodigoCategoria = "CELULAR", Nome = "Celular", Descricao = "Plano de celular" },
            new Categoria { CodigoCategoria = "TV", Nome = "TV por Assinatura", Descricao = "Netflix, HBO, etc" },
            
            // ALIMENTAÇÃO
            new Categoria { CodigoCategoria = "MERCADO", Nome = "Supermercado", Descricao = "Compras do mês" },
            new Categoria { CodigoCategoria = "IFOOD", Nome = "Delivery", Descricao = "Ifood, Uber Eats" },
            new Categoria { CodigoCategoria = "RESTAUR", Nome = "Restaurante", Descricao = "Refeições fora" },
            
            // TRANSPORTE
            new Categoria { CodigoCategoria = "COMBUST", Nome = "Combustível", Descricao = "Gasolina, álcool, diesel" },
            new Categoria { CodigoCategoria = "UBER", Nome = "Uber/Táxi", Descricao = "Transporte por aplicativo" },
            new Categoria { CodigoCategoria = "ONIBUS", Nome = "Ônibus/Metrô", Descricao = "Transporte público" },
            new Categoria { CodigoCategoria = "ESTACION", Nome = "Estacionamento", Descricao = "Estacionamento" },
            
            // SAÚDE
            new Categoria { CodigoCategoria = "PLANO", Nome = "Plano de Saúde", Descricao = "Mensalidade do plano" },
            new Categoria { CodigoCategoria = "FARMACIA", Nome = "Farmácia", Descricao = "Remédios e produtos" },
            new Categoria { CodigoCategoria = "MEDICO", Nome = "Consultas Médicas", Descricao = "Consultas e exames" },
            new Categoria { CodigoCategoria = "ACADEMIA", Nome = "Academia", Descricao = "Mensalidade da academia" },
            
            // EDUCAÇÃO
            new Categoria { CodigoCategoria = "CURSO", Nome = "Cursos", Descricao = "Cursos e treinamentos" },
            new Categoria { CodigoCategoria = "LIVRO", Nome = "Livros", Descricao = "Livros e materiais" },
            new Categoria { CodigoCategoria = "ESCOLA", Nome = "Escola/Faculdade", Descricao = "Mensalidade escolar" },
            
            // LAZER
            new Categoria { CodigoCategoria = "CINEMA", Nome = "Cinema", Descricao = "Ingressos de cinema" },
            new Categoria { CodigoCategoria = "SHOW", Nome = "Shows", Descricao = "Ingressos de shows" },
            new Categoria { CodigoCategoria = "VIAGEM", Nome = "Viagens", Descricao = "Passagens e hospedagem" },
            new Categoria { CodigoCategoria = "HOBBIES", Nome = "Hobbies", Descricao = "Passatempos" },
            
            // OUTROS
            new Categoria { CodigoCategoria = "ROUPA", Nome = "Roupas", Descricao = "Vestuário e calçados" },
            new Categoria { CodigoCategoria = "PRESENT", Nome = "Presentes", Descricao = "Presentes para outros" },
            new Categoria { CodigoCategoria = "DOACAO", Nome = "Doações", Descricao = "Doações para caridade" },
            new Categoria { CodigoCategoria = "EMPREST", Nome = "Empréstimos", Descricao = "Pagamento de empréstimos" },
            new Categoria { CodigoCategoria = "TRANSF", Nome = "Transferências", Descricao = "Transferência entre contas" },
            new Categoria { CodigoCategoria = "OUTROS", Nome = "Outros", Descricao = "Outras despesas" }
        };

        await _categorias.InsertManyAsync(categoriasPadrao);
        resultado.CategoriasCriadas = categoriasPadrao.Count;
        resultado.ItensCriados.Add($"Categorias: {categoriasPadrao.Count} categorias criadas");

        // 3. CONTAS PADRÃO
        var bancoItau = bancosPadrao.First(b => b.CodigoBanco == "341");
        var bancoNubank = bancosPadrao.First(b => b.CodigoBanco == "260");

        var contasPadrao = new List<Conta>
        {
            new Conta
            {
                CodigoConta = "CORRENTE001",
                Nome = "Conta Corrente Itaú",
                Tipo = TipoConta.Corrente,
                SaldoInicial = 2500.00m,
                BancoId = bancoItau.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new Conta
            {
                CodigoConta = "POUPANCA001",
                Nome = "Poupança Itaú",
                Tipo = TipoConta.Poupanca,
                SaldoInicial = 5000.00m,
                BancoId = bancoItau.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new Conta
            {
                CodigoConta = "NUBANK001",
                Nome = "Conta Nubank",
                Tipo = TipoConta.Corrente,
                SaldoInicial = 1500.00m,
                BancoId = bancoNubank.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new Conta
            {
                CodigoConta = "CARTAO001",
                Nome = "Cartão de Crédito",
                Tipo = TipoConta.Corrente,
                SaldoInicial = -1200.00m, // Saldo negativo para cartão
                BancoId = bancoItau.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        };

        await _contas.InsertManyAsync(contasPadrao);
        resultado.ContasCriadas = contasPadrao.Count;
        resultado.ItensCriados.AddRange(contasPadrao.Select(c => $"Conta: {c.Nome}"));
    }

    // 🔹 CRIAR DADOS DE EXEMPLO (LANÇAMENTOS E CUSTOS)
    private async Task CriarDadosExemplo(ResultadoReset resultado)
    {
        var hoje = DateTime.Now;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var categorias = await _categorias.Find(_ => true).ToListAsync();
        var contas = await _contas.Find(_ => true).ToListAsync();

        // 1. CUSTOS FIXOS MENSAlS
        var custosFixos = new List<CustosFixos>
        {
            new CustosFixos
            {
                Descricao = "Aluguel Apartamento",
                Valor = 1500.00m,
                Vencimento = new DateTime(hoje.Year, hoje.Month, 5),
                CategoriaId = categorias.First(c => c.CodigoCategoria == "ALUGUEL").Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new CustosFixos
            {
                Descricao = "Condomínio",
                Valor = 350.00m,
                Vencimento = new DateTime(hoje.Year, hoje.Month, 10),
                CategoriaId = categorias.First(c => c.CodigoCategoria == "CONDOM").Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new CustosFixos
            {
                Descricao = "Energia Elétrica",
                Valor = 180.00m,
                Vencimento = new DateTime(hoje.Year, hoje.Month, 15),
                CategoriaId = categorias.First(c => c.CodigoCategoria == "ENERGIA").Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new CustosFixos
            {
                Descricao = "Internet 300MB",
                Valor = 99.90m,
                Vencimento = new DateTime(hoje.Year, hoje.Month, 12),
                CategoriaId = categorias.First(c => c.CodigoCategoria == "INTERNET").Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new CustosFixos
            {
                Descricao = "Plano de Saúde Unimed",
                Valor = 420.00m,
                Vencimento = new DateTime(hoje.Year, hoje.Month, 8),
                CategoriaId = categorias.First(c => c.CodigoCategoria == "PLANO").Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new CustosFixos
            {
                Descricao = "Academia Smart Fit",
                Valor = 89.90m,
                Vencimento = new DateTime(hoje.Year, hoje.Month, 3),
                CategoriaId = categorias.First(c => c.CodigoCategoria == "ACADEMIA").Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        };

        await _custosFixos.InsertManyAsync(custosFixos);
        resultado.CustosFixosCriados = custosFixos.Count;
        resultado.ItensCriados.Add($"Custos Fixos: {custosFixos.Count} criados");

        // 2. LANÇAMENTOS DO MÊS ATUAL (MISTURA DE PAGOS E PENDENTES)
        var contaCorrente = contas.First(c => c.Tipo == TipoConta.Corrente && c.SaldoInicial > 0);
        var cartaoCredito = contas.First(c => c.SaldoInicial < 0);
        var categoriaSalario = categorias.First(c => c.CodigoCategoria == "SALARIO");
        var categoriaMercado = categorias.First(c => c.CodigoCategoria == "MERCADO");
        var categoriaCombust = categorias.First(c => c.CodigoCategoria == "COMBUST");
        var categoriaIfood = categorias.First(c => c.CodigoCategoria == "IFOOD");
        var categoriaUber = categorias.First(c => c.CodigoCategoria == "UBER");

        var lancamentos = new List<Lancamento>
        {
            // RECEITAS (SALÁRIO - JÁ PAGO)
            new Lancamento
            {
                Descricao = "Salário Empresa XPTO",
                Valor = 4500.00m,
                Data = new DateTime(hoje.Year, hoje.Month, 5),
                Tipo = TipoLancamento.Entrada,
                Status = StatusPagamento.Pago,
                CategoriaId = categoriaSalario.Id!,
                ContaId = contaCorrente.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            
            // DESPESAS PAGAS
            new Lancamento
            {
                Descricao = "Supermercado Extra",
                Valor = 320.50m,
                Data = new DateTime(hoje.Year, hoje.Month, 3),
                Tipo = TipoLancamento.Saida,
                Status = StatusPagamento.Pago,
                CategoriaId = categoriaMercado.Id!,
                ContaId = cartaoCredito.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new Lancamento
            {
                Descricao = "Posto Shell - Gasolina",
                Valor = 180.00m,
                Data = new DateTime(hoje.Year, hoje.Month, 10),
                Tipo = TipoLancamento.Saida,
                Status = StatusPagamento.Pago,
                CategoriaId = categoriaCombust.Id!,
                ContaId = cartaoCredito.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            
            // DESPESAS PENDENTES (PARA VENCER)
            new Lancamento
            {
                Descricao = "Ifood - Jantar",
                Valor = 65.80m,
                Data = new DateTime(hoje.Year, hoje.Month, 25),
                Tipo = TipoLancamento.Saida,
                Status = StatusPagamento.Pendente,
                CategoriaId = categoriaIfood.Id!,
                ContaId = cartaoCredito.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new Lancamento
            {
                Descricao = "Uber - Trabalho",
                Valor = 28.50m,
                Data = new DateTime(hoje.Year, hoje.Month, 28),
                Tipo = TipoLancamento.Saida,
                Status = StatusPagamento.Pendente,
                CategoriaId = categoriaUber.Id!,
                ContaId = cartaoCredito.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            
            // DESPESAS ATRASADAS (VENCIDAS)
            new Lancamento
            {
                Descricao = "Farmácia Droga Raia",
                Valor = 45.30m,
                Data = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(-1), // Mês passado
                Tipo = TipoLancamento.Saida,
                Status = StatusPagamento.Pendente,
                CategoriaId = categorias.First(c => c.CodigoCategoria == "FARMACIA").Id!,
                ContaId = cartaoCredito.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new Lancamento
            {
                Descricao = "Cinema - Vingadores",
                Valor = 72.00m,
                Data = new DateTime(hoje.Year, hoje.Month, 15).AddMonths(-1), // Mês passado
                Tipo = TipoLancamento.Saida,
                Status = StatusPagamento.Pendente,
                CategoriaId = categorias.First(c => c.CodigoCategoria == "CINEMA").Id!,
                ContaId = cartaoCredito.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        };

        await _lancamentos.InsertManyAsync(lancamentos);
        resultado.LancamentosCriados = lancamentos.Count;
        resultado.ItensCriados.Add($"Lançamentos: {lancamentos.Count} criados");

        // 3. RECEITAS ADICIONAIS
        var receitas = new List<Receita>
        {
            new Receita
            {
                Descricao = "Freelance Site Empresa ABC",
                Valor = 1200.00m,
                Data = new DateTime(hoje.Year, hoje.Month, 20),
                Status = StatusPagamento.Pendente,
                CategoriaId = categorias.First(c => c.CodigoCategoria == "FREELA").Id!,
                ContaId = contaCorrente.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new Receita
            {
                Descricao = "Bônus de Performance",
                Valor = 800.00m,
                Data = new DateTime(hoje.Year, hoje.Month, 28),
                Status = StatusPagamento.Pendente,
                CategoriaId = categorias.First(c => c.CodigoCategoria == "BONUS").Id!,
                ContaId = contaCorrente.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        };

        await _receitas.InsertManyAsync(receitas);
        resultado.ReceitasCriadas = receitas.Count;
        resultado.ItensCriados.Add($"Receitas: {receitas.Count} criadas");

        // CALCULAR TOTAL
        resultado.TotalItens = resultado.BancosCriados + 
                               resultado.CategoriasCriadas + 
                               resultado.ContasCriadas + 
                               resultado.CustosFixosCriados + 
                               resultado.LancamentosCriados + 
                               resultado.ReceitasCriadas;
    }

    // 🔹 FUNÇÃO SIMPLES: APENAS LIMPAR TUDO
    public async Task<ResultadoSimples> LimparTudo()
    {
        var resultado = new ResultadoSimples
        {
            DataProcessamento = DateTime.Now
        };

        try
        {
            await _bancos.DeleteManyAsync(_ => true);
            await _contas.DeleteManyAsync(_ => true);
            await _categorias.DeleteManyAsync(_ => true);
            await _custosFixos.DeleteManyAsync(_ => true);
            await _lancamentos.DeleteManyAsync(_ => true);
            await _receitas.DeleteManyAsync(_ => true);

            resultado.Sucesso = true;
            resultado.Mensagem = "Banco limpo com sucesso!";
            resultado.Detalhes = "Todas as collections foram esvaziadas.";
        }
        catch (Exception ex)
        {
            resultado.Sucesso = false;
            resultado.Mensagem = $"Erro ao limpar banco: {ex.Message}";
        }

        return resultado;
    }

    // 🔹 FUNÇÃO SIMPLES: APENAS CRIAR DADOS PADRÃO
    public async Task<ResultadoSimples> CriarDadosPadraoApenas()
    {
        var resultado = new ResultadoSimples
        {
            DataProcessamento = DateTime.Now
        };

        try
        {
            var resetResult = await LimparECriarTudo(false);
            
            resultado.Sucesso = resetResult.Sucesso;
            resultado.Mensagem = resetResult.Mensagem;
            resultado.Detalhes = $"Criados: {resetResult.TotalItens} itens totais";
        }
        catch (Exception ex)
        {
            resultado.Sucesso = false;
            resultado.Mensagem = $"Erro ao criar dados: {ex.Message}";
        }

        return resultado;
    }

    // 🔹 VERIFICAR STATUS DO BANCO
    public async Task<StatusBanco> VerificarStatus()
    {
        var status = new StatusBanco
        {
            DataVerificacao = DateTime.Now
        };

        try
        {
            status.Bancos = await _bancos.CountDocumentsAsync(_ => true);
            status.Contas = await _contas.CountDocumentsAsync(_ => true);
            status.Categorias = await _categorias.CountDocumentsAsync(_ => true);
            status.CustosFixos = await _custosFixos.CountDocumentsAsync(_ => true);
            status.Lancamentos = await _lancamentos.CountDocumentsAsync(_ => true);
            status.Receitas = await _receitas.CountDocumentsAsync(_ => true);

            status.TotalItens = status.Bancos + status.Contas + status.Categorias + 
                               status.CustosFixos + status.Lancamentos + status.Receitas;
            
            status.SistemaPronto = status.Bancos > 0 && status.Contas > 0 && status.Categorias > 0;
            status.Mensagem = status.SistemaPronto ? 
                "Sistema pronto para uso" : 
                "Sistema precisa de configuração inicial";
        }
        catch (Exception ex)
        {
            status.Erro = ex.Message;
        }

        return status;
    }
}

// 🔹 MODELOS PARA RESULTADOS
public class ResultadoReset
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = "";
    public string? Erro { get; set; }
    public DateTime DataProcessamento { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public TimeSpan TempoExecucao { get; set; }
    
    public List<string> CollectionsLimpas { get; set; } = new List<string>();
    public int TotalLimpado { get; set; }
    
    public int BancosCriados { get; set; }
    public int CategoriasCriadas { get; set; }
    public int ContasCriadas { get; set; }
    public int CustosFixosCriados { get; set; }
    public int LancamentosCriados { get; set; }
    public int ReceitasCriadas { get; set; }
    public int TotalItens { get; set; }
    
    public List<string> ItensCriados { get; set; } = new List<string>();
}

public class ResultadoSimples
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = "";
    public string? Detalhes { get; set; }
    public DateTime DataProcessamento { get; set; }
}

public class StatusBanco
{
    public DateTime DataVerificacao { get; set; }
    public long Bancos { get; set; }
    public long Contas { get; set; }
    public long Categorias { get; set; }
    public long CustosFixos { get; set; }
    public long Lancamentos { get; set; }
    public long Receitas { get; set; }
    public long TotalItens { get; set; }
    public bool SistemaPronto { get; set; }
    public string Mensagem { get; set; } = "";
    public string? Erro { get; set; }
}
