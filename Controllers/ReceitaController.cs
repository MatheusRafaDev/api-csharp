using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class ReceitasController : ControllerBase
{
    private readonly IMongoCollection<Receita> _collection;

    public ReceitasController(IMongoDatabase database)
    {
        _collection = database.GetCollection<Receita>("Receitas");
    }

    // 🔹 LISTAR TODAS
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var receitas = await _collection.Find(_ => true).ToListAsync();
        return Ok(receitas);
    }

    // 🔹 BUSCAR POR ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var receita = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (receita == null) return NotFound();

        return Ok(receita);
    }

    // 🔹 CRIAR
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Receita receita)
    {
        await _collection.InsertOneAsync(receita);
        return CreatedAtAction(nameof(GetById), new { id = receita.Id }, receita);
    }

    // 🔹 ATUALIZAR
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] Receita receita)
    {
        receita.Id = id;

        var result = await _collection.ReplaceOneAsync(x => x.Id == id, receita);
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

    // 🔹 TOTAL DE RECEITAS
    [HttpGet("total")]
    public async Task<IActionResult> Total()
    {
        var total = await _collection
            .Aggregate()
            .Group(x => 1, g => new { Total = g.Sum(x => x.Valor) })
            .FirstOrDefaultAsync();

        return Ok(total?.Total ?? 0);
    }
}
