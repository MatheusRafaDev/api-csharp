using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class RelatorioController : ControllerBase
{
    private readonly LancamentoService _service;

    public RelatorioController(LancamentoService service)
    {
        _service = service;
    }

    // 🔹 RELATÓRIO COMPLETO
    [HttpGet]
    public async Task<IActionResult> GetRelatorio([FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim)
    {
        try
        {
            var relatorio = await _service.GerarRelatorio(dataInicio, dataFim);
            return Ok(relatorio);
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao gerar relatório: {ex.Message}");
        }
    }

    // 🔹 ALERTAS E REGRAS
    [HttpGet("alertas")]
    public async Task<IActionResult> GetAlertas()
    {
        try
        {
            var regras = await _service.VerificarRegras();
            return Ok(regras);
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao verificar alertas: {ex.Message}");
        }
    }

    // 🔹 ATUALIZAR STATUS AUTOMÁTICO
    [HttpPost("atualizar-status")]
    public async Task<IActionResult> AtualizarStatus()
    {
        try
        {
            var atualizados = await _service.AtualizarStatusAutomatico();
            return Ok(new { Mensagem = $"{atualizados} lançamentos atualizados automaticamente." });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao atualizar status: {ex.Message}");
        }
    }

    // 🔹 RESUMO RÁPIDO
    [HttpGet("resumo")]
    public async Task<IActionResult> GetResumo([FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim)
    {
        try
        {
            var relatorio = await _service.GerarRelatorio(dataInicio, dataFim);
            
            var resumo = new
            {
                relatorio.Resumo,
                TotalVencidos = relatorio.LancamentosVencidos.Count,
                TotalCustosVencidos = relatorio.CustosFixosVencidos.Count,
                TotalAlertas = relatorio.Alertas.Count,
                PrimeiroAlerta = relatorio.Alertas.FirstOrDefault()
            };

            return Ok(resumo);
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao gerar resumo: {ex.Message}");
        }
    }

    // 🔹 LANÇAMENTOS VENCIDOS
    [HttpGet("vencidos")]
    public async Task<IActionResult> GetVencidos()
    {
        try
        {
            var relatorio = await _service.GerarRelatorio();
            
            var vencidos = new
            {
                relatorio.LancamentosVencidos,
                relatorio.CustosFixosVencidos,
                relatorio.CustosFixosProximos,
                TotalVencidos = relatorio.LancamentosVencidos.Count,
                TotalCustosVencidos = relatorio.CustosFixosVencidos.Count,
                ValorTotalVencido = relatorio.LancamentosVencidos.Sum(x => x.Lancamento.Valor) +
                                   relatorio.CustosFixosVencidos.Sum(x => x.CustoFixo.Valor)
            };

            return Ok(vencidos);
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao buscar vencidos: {ex.Message}");
        }
    }

    // 🔹 DASHBOARD COMPLETO
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int mes = 0, [FromQuery] int ano = 0)
    {
        try
        {
            DateTime dataInicio, dataFim;

            if (mes == 0 || ano == 0)
            {
                // Mês atual
                var hoje = DateTime.Now;
                dataInicio = new DateTime(hoje.Year, hoje.Month, 1);
                dataFim = dataInicio.AddMonths(1).AddSeconds(-1);
            }
            else
            {
                dataInicio = new DateTime(ano, mes, 1);
                dataFim = dataInicio.AddMonths(1).AddSeconds(-1);
            }

            var relatorio = await _service.GerarRelatorio(dataInicio, dataFim);
            var regras = await _service.VerificarRegras();

            var dashboard = new
            {
                relatorio.Resumo,
                relatorio.Periodo,
                AlertasCriticos = regras.Where(r => r.Tipo == TipoAlerta.Critico).ToList(),
                AlertasNormais = regras.Where(r => r.Tipo != TipoAlerta.Critico).ToList(),
                TopCategorias = relatorio.ResumoPorCategoria.Take(5).ToList(),
                TopContas = relatorio.ResumoPorConta.Take(5).ToList(),
                TotalVencidos = relatorio.LancamentosVencidos.Count,
                TotalPendentes = relatorio.Resumo.LancamentosPendentes,
                UltimosLancamentos = relatorio.Lancamentos
                    .OrderByDescending(x => x.Lancamento.Data)
                    .Take(10)
                    .ToList()
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao gerar dashboard: {ex.Message}");
        }
    }
}
