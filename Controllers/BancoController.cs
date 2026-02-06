using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class BancoController : ControllerBase
{
    private readonly IMongoCollection<Banco> _collection;

    public BancoController(IMongoDatabase database)
    {
        _collection = database.GetCollection<Banco>("Banco");
    }

    // 🔹 LISTAR TODOS
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var bancos = await _collection.Find(_ => true).ToListAsync();
        return Ok(bancos);
    }

    // 🔹 BUSCAR POR ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var banco = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (banco == null) return NotFound();
        return Ok(banco);
    }

    // 🔹 CRIAR
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Banco banco)
    {
        await _collection.InsertOneAsync(banco);
        return CreatedAtAction(nameof(GetById), new { id = banco.Id }, banco);
    }

    // 🔹 ATUALIZAR
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] Banco banco)
    {
        banco.Id = id;

        var result = await _collection.ReplaceOneAsync(x => x.Id == id, banco);
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
