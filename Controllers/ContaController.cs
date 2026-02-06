using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class ContaController : ControllerBase
{
    private readonly IMongoCollection<Conta> _collection;

    public ContaController(IMongoDatabase database)
    {
        _collection = database.GetCollection<Conta>("Contas");
    }

    // 🔹 LISTAR TODAS
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var contas = await _collection.Find(_ => true).ToListAsync();
        return Ok(contas);
    }

    // 🔹 BUSCAR POR ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var conta = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (conta == null) return NotFound();
        return Ok(conta);
    }

    // 🔹 CRIAR
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Conta conta)
    {
        await _collection.InsertOneAsync(conta);
        return CreatedAtAction(nameof(GetById), new { id = conta.Id }, conta);
    }

    // 🔹 ATUALIZAR
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] Conta conta)
    {
        conta.Id = id;

        var result = await _collection.ReplaceOneAsync(x => x.Id == id, conta);
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
}
