
using Microsoft.AspNetCore.Mvc;
using System;
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
                
                Detalhes = resultado.ItensCriados.Take(20).ToList(), // Mostra apenas 20 itens
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
            var resultado = await _service.LimparTudo();
            
            return Ok(new
            {
                resultado.Sucesso,
                resultado.Mensagem,
                resultado.DataProcessamento,
                resultado.Detalhes,
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
            var resultado = await _service.CriarDadosPadraoApenas();
            
            return Ok(new
            {
                resultado.Sucesso,
                resultado.Mensagem,
                resultado.DataProcessamento,
                resultado.Detalhes,
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
            var status = await _service.VerificarStatus();
            
            return Ok(new
            {
                status.DataVerificacao,
                status.SistemaPronto,
                status.Mensagem,
                
                Contagens = new
                {
                    Bancos = status.Bancos,
                    Contas = status.Contas,
                    Categorias = status.Categorias,
                    CustosFixos = status.CustosFixos,
                    Lancamentos = status.Lancamentos,
                    Receitas = status.Receitas,
                    Total = status.TotalItens
                },
                
                Recomendacao = status.SistemaPronto ? 
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
            // 1. Limpar tudo
            await _service.LimparTudo();
            
            // 2. Criar dados padrão
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
            // Limpar apenas dados transacionais, manter configurações se existirem
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
            // Primeiro criar base
            await _service.LimparECriarTudo(false);
            
            // Aqui você poderia adicionar lógica para criar muitos dados
            // Mas já temos uma boa quantidade no reset-completo
            
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
