using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class LancamentosController : ControllerBase
{
    private readonly IMongoCollection<Lancamento> _collection;

    public LancamentosController(IMongoDatabase database)
    {
        _collection = database.GetCollection<Lancamento>("Lancamentos");
    }

    // 🔹 LISTAR TODOS
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var lancamentos = await _collection.Find(_ => true).ToListAsync();
        return Ok(lancamentos);
    }

    // 🔹 BUSCAR POR ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var lancamento = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (lancamento == null) return NotFound();
        return Ok(lancamento);
    }

    // 🔹 CRIAR
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Lancamento lancamento)
    {
        await _collection.InsertOneAsync(lancamento);
        return CreatedAtAction(nameof(GetById), new { id = lancamento.Id }, lancamento);
    }

    // 🔹 ATUALIZAR
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] Lancamento lancamento)
    {
        lancamento.Id = id;

        var result = await _collection.ReplaceOneAsync(x => x.Id == id, lancamento);
        if (result.MatchedCount == 0) return NotFound();

        return NoContent();
    }

    // 🔹 DELETAR
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _collection.DeleteOneAsync(x => x.Id == id);
        if (result.DeletedCount == 0) return NotFound();

        return NoContent();
    }

    // 🔹 SALDO TOTAL (ENTRADAS - SAÍDAS)
    [HttpGet("saldo")]
    public async Task<IActionResult> Saldo()
    {
        var lancamentos = await _collection.Find(_ => true).ToListAsync();

        var entradas = lancamentos
            .Where(x => x.Tipo == "Entrada")
            .Sum(x => x.Valor);

        var saidas = lancamentos
            .Where(x => x.Tipo == "Saída")
            .Sum(x => x.Valor);

        return Ok(new
        {
            Entradas = entradas,
            Saidas = saidas,
            Saldo = entradas - saidas
        });
    }
}
