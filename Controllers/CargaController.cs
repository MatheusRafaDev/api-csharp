using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class CargaController : ControllerBase
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Banco> _bancos;
    private readonly IMongoCollection<Conta> _contas;
    private readonly IMongoCollection<Categoria> _categorias;
    private readonly IMongoCollection<CustosFixos> _custosFixos;
    private readonly IMongoCollection<Receita> _receitas;
    private readonly IMongoCollection<Lancamento> _lancamentos;

    public CargaController(IMongoDatabase database)
    {
        _database = database;
        _bancos = database.GetCollection<Banco>("Bancos");
        _contas = database.GetCollection<Conta>("Contas");
        _categorias = database.GetCollection<Categoria>("Categorias");
        _custosFixos = database.GetCollection<CustosFixos>("CustosFixos");
        _receitas = database.GetCollection<Receita>("Receitas");
        _lancamentos = database.GetCollection<Lancamento>("Lancamentos");
    }

    // 🔹 Carga via JSON
    [HttpPost("json")]
    public async Task<IActionResult> CargaViaJson([FromBody] CargaRequest request)
    {
        if (request == null)
            return BadRequest("O corpo da requisição não pode ser nulo.");

        // Limpar dados se necessário
        if (request.LimparAntes)
            await LimparTudo();

        // Inserir bancos
        if (request.Bancos != null && request.Bancos.Any())
            await _bancos.InsertManyAsync(request.Bancos);

        // Inserir categorias
        if (request.Categorias != null && request.Categorias.Any())
            await _categorias.InsertManyAsync(request.Categorias);


        // Inserir custos fixos
        if (request.CustosFixos != null && request.CustosFixos.Any())
            await _custosFixos.InsertManyAsync(request.CustosFixos);

        // Inserir receitas
        if (request.Receitas != null && request.Receitas.Any())
            await _receitas.InsertManyAsync(request.Receitas);

        // Inserir lançamentos
        if (request.Lancamentos != null && request.Lancamentos.Any())
            await _lancamentos.InsertManyAsync(request.Lancamentos);

        return Ok(new { Mensagem = "Carga concluída com sucesso!" });
    }

    // 🔹 Limpar todas as collections
    private async Task LimparTudo()
    {
        await _bancos.DeleteManyAsync(_ => true);
        await _contas.DeleteManyAsync(_ => true);
        await _categorias.DeleteManyAsync(_ => true);
        await _custosFixos.DeleteManyAsync(_ => true);
        await _receitas.DeleteManyAsync(_ => true);
        await _lancamentos.DeleteManyAsync(_ => true);
    }

    // 🔹 Status das collections
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        return Ok(new
        {
            Bancos = await _bancos.CountDocumentsAsync(_ => true),
            Contas = await _contas.CountDocumentsAsync(_ => true),
            Categorias = await _categorias.CountDocumentsAsync(_ => true),
            CustosFixos = await _custosFixos.CountDocumentsAsync(_ => true),
            Receitas = await _receitas.CountDocumentsAsync(_ => true),
            Lancamentos = await _lancamentos.CountDocumentsAsync(_ => true)
        });
    }
}

// 🔹 Request para carga
public class CargaRequest
{
    public bool LimparAntes { get; set; } = false;
    public List<Banco>? Bancos { get; set; }
    public List<Conta>? Contas { get; set; }
    public List<Categoria>? Categorias { get; set; }
    public List<CustosFixos>? CustosFixos { get; set; }
    public List<Receita>? Receitas { get; set; }
    public List<Lancamento>? Lancamentos { get; set; }
}
