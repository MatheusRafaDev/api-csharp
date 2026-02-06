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
        // Validação de código único
        var existente = await _collection.Find(b => b.CodigoBanco == banco.CodigoBanco).FirstOrDefaultAsync();
        if (existente != null)
            return BadRequest($"Código do banco '{banco.CodigoBanco}' já existe.");

        await _collection.InsertOneAsync(banco);
        return CreatedAtAction(nameof(GetById), new { id = banco.Id }, banco);
    }

    // 🔹 ATUALIZAR
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] Banco banco)
    {
        // Validação de código único (ignora o próprio registro)
        var existente = await _collection.Find(b => b.CodigoBanco == banco.CodigoBanco && b.Id != id).FirstOrDefaultAsync();
        if (existente != null)
            return BadRequest($"Código do banco '{banco.CodigoBanco}' já existe em outro registro.");

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

    // 🔹 CRIAR VÁRIOS (LOTE)
    [HttpPost("Carga")]
    public async Task<IActionResult> PostCarga([FromBody] List<Banco> bancos)
    {
        if (bancos == null || bancos.Count == 0)
            return BadRequest("A lista de bancos está vazia.");

        // Valida duplicados dentro da própria lista
        var duplicadosInternos = bancos.GroupBy(b => b.CodigoBanco)
                                       .Where(g => g.Count() > 1)
                                       .Select(g => g.Key)
                                       .ToList();
        if (duplicadosInternos.Any())
            return BadRequest($"Existem códigos duplicados na lista: {string.Join(", ", duplicadosInternos)}");

        // Valida duplicados no banco
        var codigos = bancos.Select(b => b.CodigoBanco).ToList();
        var existentes = await _collection.Find(b => codigos.Contains(b.CodigoBanco)).ToListAsync();
        if (existentes.Any())
            return BadRequest($"Os seguintes códigos já existem no banco: {string.Join(", ", existentes.Select(e => e.CodigoBanco))}");

        await _collection.InsertManyAsync(bancos);
        return Ok(new { Mensagem = $"{bancos.Count} bancos inseridos com sucesso.", Bancos = bancos });
    }
}
