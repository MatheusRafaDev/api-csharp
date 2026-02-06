using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class CustosFixosController : ControllerBase
{
    private readonly IMongoCollection<CustosFixos> _collection;

    public CustosFixosController(IMongoDatabase database)
    {
        _collection = database.GetCollection<CustosFixos>("CustosFixos");
    }

    // 🔹 LISTAR TODOS
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var custos = await _collection.Find(_ => true).ToListAsync();
        return Ok(custos);
    }

    // 🔹 BUSCAR POR ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var custo = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (custo == null) return NotFound();
        return Ok(custo);
    }

    // 🔹 CRIAR
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CustosFixos custo)
    {
        await _collection.InsertOneAsync(custo);
        return CreatedAtAction(nameof(GetById), new { id = custo.Id }, custo);
    }

    // 🔹 ATUALIZAR
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] CustosFixos custo)
    {
        var result = await _collection.ReplaceOneAsync(x => x.Id == id, custo);
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

    // 🔹 TOTAL DE CUSTOS FIXOS
    [HttpGet("total")]
    public async Task<IActionResult> Total()
    {
        var total = await _collection
            .Find(_ => true)
            .Project(x => x.Valor)
            .ToListAsync();

        return Ok(new { Total = total.Sum() });
    }
}
