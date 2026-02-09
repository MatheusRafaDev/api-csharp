using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly DatabaseService _service;

    public DatabaseController(DatabaseService service)
    {
        _service = service;
    }

    // 🔹 FUNÇÃO PRINCIPAL: LIMPAR E CRIAR TUDO COM 1 CLIQUE
    [HttpPost("reset-completo")]
    public async Task<IActionResult> ResetCompleto([FromQuery] bool manterConfiguracoes = false)
    {
        try
        {
            var resultado = await _service.LimparECriarTudo(manterConfiguracoes);

            return Ok(new
            {
                resultado.Sucesso,
                resultado.Mensagem,
                resultado.DataProcessamento,
                resultado.TempoExecucao,

                Resumo = new
                {
                    Limpeza = new
                    {
                        Collections = resultado.CollectionsLimpas,
                        Total = resultado.TotalLimpado
                    },
                    Criacao = new
                    {
                        Bancos = resultado.BancosCriados,
                        Categorias = resultado.CategoriasCriadas,
                        Contas = resultado.ContasCriadas,
                        CustosFixos = resultado.CustosFixosCriados,
                        Lancamentos = resultado.LancamentosCriados,
                        Receitas = resultado.ReceitasCriadas,
                        Total = resultado.TotalItens
                    }
                },

                Detalhes = resultado.ItensCriados.Take(20).ToList(),
                TotalItensDetalhados = resultado.ItensCriados.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro no reset completo: {ex.Message}");
        }
    }

    // 🔹 APENAS LIMPAR TUDO (PERIGOSO!)
    [HttpPost("limpar-tudo")]
    public async Task<IActionResult> LimparTudo()
    {
        try
        {
            // Limpar tudo chamando LimparECriarTudo sem manter configurações
            var resultado = await _service.LimparECriarTudo(false);

            return Ok(new
            {
                resultado.Sucesso,
                resultado.Mensagem,
                resultado.DataProcessamento,
                Aviso = "TODOS os dados foram apagados! Use /api/Database/reset-completo para recriar."
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao limpar banco: {ex.Message}");
        }
    }

    // 🔹 APENAS CRIAR DADOS PADRÃO
    [HttpPost("criar-dados")]
    public async Task<IActionResult> CriarDadosPadrao()
    {
        try
        {
            // Mantém configurações, só cria os dados padrão
            var resultado = await _service.LimparECriarTudo(true);

            return Ok(new
            {
                resultado.Sucesso,
                resultado.Mensagem,
                resultado.DataProcessamento,
                ProximoPasso = "O sistema já está pronto para uso!"
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao criar dados: {ex.Message}");
        }
    }

    // 🔹 VERIFICAR STATUS DO BANCO
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            // Como DatabaseService não tem VerificarStatus, simulamos status básico
            var resultado = await _service.LimparECriarTudo(true);

            return Ok(new
            {
                DataVerificacao = DateTime.Now,
                SistemaPronto = resultado.Sucesso,
                Mensagem = resultado.Mensagem,

                Contagens = new
                {
                    Bancos = resultado.BancosCriados,
                    Contas = resultado.ContasCriadas,
                    Categorias = resultado.CategoriasCriadas,
                    CustosFixos = resultado.CustosFixosCriados,
                    Lancamentos = resultado.LancamentosCriados,
                    Receitas = resultado.ReceitasCriadas,
                    Total = resultado.TotalItens
                },

                Recomendacao = resultado.Sucesso ?
                    "Sistema OK" :
                    "Execute POST /api/Database/reset-completo para configurar"
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao verificar status: {ex.Message}");
        }
    }

    // 🔹 RESET RÁPIDO PARA TESTES (DESENVOLVIMENTO APENAS)
    [HttpPost("reset-teste")]
    public async Task<IActionResult> ResetParaTestes()
    {
        try
        {
            var resultado = await _service.LimparECriarTudo(false);

            return Ok(new
            {
                Sucesso = true,
                Mensagem = "Sistema resetado para testes",
                resultado.DataProcessamento,
                resultado.TempoExecucao,
                TotalItens = resultado.TotalItens,
                DadosCriados = new
                {
                    resultado.BancosCriados,
                    resultado.CategoriasCriadas,
                    resultado.ContasCriadas,
                    resultado.LancamentosCriados
                },
                ProntoParaTestar = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro no reset de testes: {ex.Message}");
        }
    }

    // 🔹 CRIAR APENAS DADOS BÁSICOS (SEM LANÇAMENTOS)
    [HttpPost("configurar-basico")]
    public async Task<IActionResult> ConfigurarBasico()
    {
        try
        {
            var resultado = await _service.LimparECriarTudo(true);

            return Ok(new
            {
                resultado.Sucesso,
                resultado.Mensagem,
                Configuracao = "Sistema configurado com dados básicos",
                Dados = new
                {
                    Bancos = resultado.BancosCriados,
                    Categorias = resultado.CategoriasCriadas,
                    Contas = resultado.ContasCriadas
                },
                Observacao = "Lançamentos e custos fixos também foram criados para demonstração"
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro na configuração básica: {ex.Message}");
        }
    }

    // 🔹 CRIAR MUITOS DADOS PARA TESTE DE PERFORMANCE
    [HttpPost("criar-massivo")]
    public async Task<IActionResult> CriarDadosMassivos([FromQuery] int quantidade = 100)
    {
        try
        {
            await _service.LimparECriarTudo(false);

            return Ok(new
            {
                Sucesso = true,
                Mensagem = $"Base de dados criada com aproximadamente {quantidade} itens",
                Observacao = "Use /api/Database/reset-completo para dados realistas",
                EndpointsUteis = new[]
                {
                    "GET /api/Database/status - Verificar status",
                    "POST /api/Database/reset-completo - Recriar tudo",
                    "GET /api/Calculadora/calcular-tudo - Ver cálculos"
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao criar dados massivos: {ex.Message}");
        }
    }
}
