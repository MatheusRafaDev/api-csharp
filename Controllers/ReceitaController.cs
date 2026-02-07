using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class ReceitasController : ControllerBase
{
    private readonly IMongoCollection<Receita> _receitas;
    private readonly IMongoCollection<Categoria> _categorias;
    private readonly IMongoCollection<Conta> _contas;

    public ReceitasController(IMongoDatabase database)
    {
        _receitas = database.GetCollection<Receita>("Receita");
        _categorias = database.GetCollection<Categoria>("Categoria");
        _contas = database.GetCollection<Conta>("Conta");
    }

    // 🔹 LISTAR
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var lista = await _receitas.Find(_ => true).ToListAsync();
        return Ok(lista);
    }

    // 🔹 BUSCAR POR ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var receita = await _receitas.Find(r => r.Id == id).FirstOrDefaultAsync();
        if (receita == null) return NotFound();
        return Ok(receita);
    }

    // 🔹 CRIAR (POR CÓDIGO)
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReceitaInput input)
    {
        var codigoCategoria = input.CodigoCategoria.Trim();
        var codigoConta = input.CodigoConta.Trim();

        var categoria = await _categorias
            .Find(c => c.CodigoCategoria == codigoCategoria)
            .FirstOrDefaultAsync();

        if (categoria == null)
            return BadRequest($"Categoria com código '{codigoCategoria}' não encontrada.");

        var conta = await _contas
            .Find(c => c.CodigoConta == codigoConta)
            .FirstOrDefaultAsync();

        if (conta == null)
            return BadRequest($"Conta com código '{codigoConta}' não encontrada.");

        var receita = new Receita
        {
            Descricao = input.Descricao,
            Valor = input.Valor,
            Data = input.Data,
            Status = input.Status,
            CategoriaId = categoria.Id!,
            ContaId = conta.Id!,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _receitas.InsertOneAsync(receita);
        return CreatedAtAction(nameof(GetById), new { id = receita.Id }, receita);
    }

    // 🔹 ATUALIZAR (MESMO PADRÃO)
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] ReceitaInput input)
    {
        var codigoCategoria = input.CodigoCategoria.Trim();
        var codigoConta = input.CodigoConta.Trim();

        var categoria = await _categorias
            .Find(c => c.CodigoCategoria == codigoCategoria)
            .FirstOrDefaultAsync();

        if (categoria == null)
            return BadRequest($"Categoria com código '{codigoCategoria}' não encontrada.");

        var conta = await _contas
            .Find(c => c.CodigoConta == codigoConta)
            .FirstOrDefaultAsync();

        if (conta == null)
            return BadRequest($"Conta com código '{codigoConta}' não encontrada.");

        var receita = new Receita
        {
            Id = id,
            Descricao = input.Descricao,
            Valor = input.Valor,
            Data = input.Data,
            Status = input.Status,
            CategoriaId = categoria.Id!,
            ContaId = conta.Id!,
            UpdatedAt = DateTime.Now
        };

        var result = await _receitas.ReplaceOneAsync(r => r.Id == id, receita);
        if (result.MatchedCount == 0) return NotFound();

        return NoContent();
    }

    // 🔹 DELETAR
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _receitas.DeleteOneAsync(r => r.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return NoContent();
    }
}

public class ReceitaInput
{
    public string Descricao { get; set; } = null!;
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }

    public StatusPagamento Status { get; set; }

    // 🔹 SOMENTE CÓDIGOS
    public string CodigoCategoria { get; set; } = null!;
    public string CodigoConta { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
