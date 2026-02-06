using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class CategoriaController : ControllerBase
{
    private readonly IMongoCollection<Categoria> _collection;

    public CategoriaController(IMongoDatabase database)
    {
        _collection = database.GetCollection<Categoria>("Categoria");
    }

    // 🔹 LISTAR TODOS
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var categorias = await _collection.Find(_ => true).ToListAsync();
        return Ok(categorias);
    }

    // 🔹 BUSCAR POR ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var categoria = await _collection.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (categoria == null) return NotFound();
        return Ok(categoria);
    }

    // 🔹 CRIAR
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Categoria categoria)
    {
        await _collection.InsertOneAsync(categoria);
        return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria);
    }

    // 🔹 ATUALIZAR
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] Categoria categoria)
    {
        categoria.Id = id;
        var result = await _collection.ReplaceOneAsync(c => c.Id == id, categoria);
        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }

    // 🔹 DELETAR
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _collection.DeleteOneAsync(c => c.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return NoContent();
    }
}
