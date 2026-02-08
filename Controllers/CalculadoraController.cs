
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class CalculadoraController : ControllerBase
{
    private readonly CalculadoraService _service;

    public CalculadoraController(CalculadoraService service)
    {
        _service = service;
    }

    // 🔹 CALCULAR TUDO (FUNÇÃO PRINCIPAL)
    [HttpGet("calcular-tudo")]
    public async Task<IActionResult> CalcularTudo()
    {
        try
        {
            var resultado = await _service.CalcularTudo();
            
            return Ok(new
            {
                resultado.Sucesso,
                resultado.Mensagem,
                resultado.DataCalculo,
                
                // Saldos
                Saldos = new
                {
                    resultado.SaldoAtual,
                    resultado.SaldoProjetado,
                    resultado.TotalEntradas,
                    resultado.TotalSaidas,
                    resultado.ReceitasPendentes,
                    resultado.DespesasPendentes
                },
                
                // Vencidos
                Vencidos = new
                {
                    resultado.TotalVencidos,
                    resultado.ValorTotalVencido,
                    LancamentosVencidos = resultado.LancamentosVencidos.Count,
                    CustosFixosVencidos = resultado.CustosFixosVencidos.Count,
                    ProximosVencimentos = resultado.ProximosVencimentos.Count
                },
                
                // Resumos
                Resumos = new
                {
                    resultado.CategoriaMaiorGasto,
                    resultado.ValorMaiorGasto,
                    resultado.ContaMaiorSaldo,
                    resultado.ValorMaiorSaldo,
                    TotalCategorias = resultado.ResumoCategorias.Count,
                    TotalContas = resultado.ResumoContas.Count
                },
                
                // Projeções
                Projecao = resultado.ProjecaoMes,
                
                // Alertas
                Alertas = resultado.Alertas,
                
                // Detalhados (opcionais)
                DetalhesLancamentosVencidos = resultado.LancamentosVencidos,
                DetalhesCustosVencidos = resultado.CustosFixosVencidos,
                DetalhesProximosVencimentos = resultado.ProximosVencimentos,
                DetalhesCategorias = resultado.ResumoCategorias,
                DetalhesContas = resultado.ResumoContas
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao calcular: {ex.Message}");
        }
    }

    // 🔹 APENAS SALDO ATUAL
    [HttpGet("saldo-atual")]
    public async Task<IActionResult> GetSaldoAtual()
    {
        try
        {
            var saldo = await _service.CalcularSaldoAtual();
            return Ok(new { SaldoAtual = saldo });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao calcular saldo: {ex.Message}");
        }
    }

    // 🔹 APENAS TOTAL VENCIDO
    [HttpGet("total-vencido")]
    public async Task<IActionResult> GetTotalVencido()
    {
        try
        {
            var total = await _service.CalcularTotalVencido();
            return Ok(new { TotalVencido = total });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao calcular vencidos: {ex.Message}");
        }
    }

    // 🔹 RESUMO RÁPIDO
    [HttpGet("resumo-rapido")]
    public async Task<IActionResult> GetResumoRapido()
    {
        try
        {
            var resumo = await _service.ResumoRapido();
            return Ok(resumo);
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao gerar resumo: {ex.Message}");
        }
    }

    // 🔹 VENCIDOS DETALHADOS
    [HttpGet("vencidos-detalhados")]
    public async Task<IActionResult> GetVencidosDetalhados()
    {
        try
        {
            var resultado = await _service.CalcularTudo();
            
            return Ok(new
            {
                Total = resultado.TotalVencidos,
                ValorTotal = resultado.ValorTotalVencido,
                LancamentosVencidos = resultado.LancamentosVencidos,
                CustosVencidos = resultado.CustosFixosVencidos,
                ProximosVencimentos = resultado.ProximosVencimentos
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao buscar vencidos: {ex.Message}");
        }
    }

    // 🔹 RESUMO POR CATEGORIA
    [HttpGet("resumo-categoria")]
    public async Task<IActionResult> GetResumoCategoria()
    {
        try
        {
            var resultado = await _service.CalcularTudo();
            
            return Ok(new
            {
                TotalCategorias = resultado.ResumoCategorias.Count,
                CategoriaMaiorGasto = resultado.ResumoCategorias.FirstOrDefault(),
                TodasCategorias = resultado.ResumoCategorias
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao gerar resumo por categoria: {ex.Message}");
        }
    }

    // 🔹 DASHBOARD SIMPLES
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var resultado = await _service.CalcularTudo();
            
            var dashboard = new
            {
                Status = new
                {
                    SaldoAtual = resultado.SaldoAtual,
                    SaldoProjetado = resultado.SaldoProjetado,
                    TotalVencidos = resultado.TotalVencidos,
                    ValorVencido = resultado.ValorTotalVencido,
                    TemAlertas = resultado.Alertas.Any()
                },
                
                Alertas = resultado.Alertas.Take(5).ToList(),
                
                TopCategorias = resultado.ResumoCategorias.Take(5).ToList(),
                
                TopVencidos = resultado.LancamentosVencidos
                    .OrderByDescending(v => v.DiasAtraso)
                    .Take(5)
                    .ToList(),
                
                Projecao = new
                {
                    resultado.ProjecaoMes.SaldoProjetadoFimMes,
                    resultado.ProjecaoMes.DiasRestantes,
                    resultado.ProjecaoMes.DespesasRestantes,
                    resultado.ProjecaoMes.ReceitasRestantes
                }
            };
            
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao gerar dashboard: {ex.Message}");
        }
    }
}
