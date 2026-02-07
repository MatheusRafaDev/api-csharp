using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class CustosFixosController : ControllerBase
{
    private readonly IMongoCollection<CustosFixos> _collection;
    private readonly IMongoCollection<Categoria> _categorias;

    public CustosFixosController(IMongoDatabase database)
    {
        _collection = database.GetCollection<CustosFixos>("CustosFixos");
        _categorias = database.GetCollection<Categoria>("Categoria");
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
    public async Task<IActionResult> Post([FromBody] CustosFixosInput input)
    {
        var codigo = input.CodigoCategoria.Trim();

        var categoria = await _categorias
            .Find(x => x.CodigoCategoria == codigo)
            .FirstOrDefaultAsync();


        if (categoria == null) return BadRequest($"Categoria com código '{input.CodigoCategoria}' não encontrada.");

        var custo = new CustosFixos
        {
            Descricao = input.Descricao,
            Valor = input.Valor,
            CategoriaId = categoria.Id!,
            CreatedAt = input.CreatedAt,
            UpdatedAt = input.UpdatedAt
        };

        await _collection.InsertOneAsync(custo);
        return CreatedAtAction(nameof(GetById), new { id = custo.Id }, custo);
    }

    // 🔹 ATUALIZAR
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] CustosFixosInput input)
    {
        var categoria = await _categorias.Find(x => x.CodigoCategoria == input.CodigoCategoria).FirstOrDefaultAsync();
        if (categoria == null) return BadRequest($"Categoria com código '{input.CodigoCategoria}' não encontrada.");

        var custo = new CustosFixos
        {
            Id = id,
            Descricao = input.Descricao,
            Valor = input.Valor,
            CategoriaId = categoria.Id!,
            CreatedAt = input.CreatedAt,
            UpdatedAt = input.UpdatedAt
        };

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

    // 🔹 CRIAR VÁRIOS (LOTE)
    [HttpPost("Carga")]
    public async Task<IActionResult> PostCarga([FromBody] List<CustosFixosInput> inputs)
    {
        if (inputs == null || !inputs.Any())
            return BadRequest("A lista de custos fixos está vazia.");

        var categorias = new List<Categoria>();
        var codigosFaltando = new List<string>();


        foreach (var codigo in inputs.Select(i => i.CodigoCategoria).Distinct())
        {


            System.Console.WriteLine(codigo);
            System.Console.WriteLine(inputs.First(i => i.CodigoCategoria == codigo).CodigoCategoria);

            var cat = await _categorias.Find(c => c.CodigoCategoria == codigo).FirstOrDefaultAsync();


            if (cat != null)
            {
                categorias.Add(cat);
            }
            else
            {
                codigosFaltando.Add(codigo);

            }
        }

        if (codigosFaltando.Any())
            return BadRequest($"As seguintes categorias não existem: {string.Join(", ", codigosFaltando)}");

        // Mapear inputs para CustosFixos
        var custos = inputs.Select(i =>
        {
            var cat = categorias.First(c => c.CodigoCategoria == i.CodigoCategoria);
            return new CustosFixos
            {
                Descricao = i.Descricao,
                Valor = i.Valor,
                CategoriaId = cat.Id!,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            };
        }).ToList();

        // Inserir todos
        await _collection.InsertManyAsync(custos);

        return Ok(new { Mensagem = $"{custos.Count} custos fixos inseridos com sucesso.", Custos = custos });
    }


}

// DTO de entrada que usa código da categoria
public class CustosFixosInput
{
    public string Descricao { get; set; } = "";
    public decimal Valor { get; set; }
    public string CodigoCategoria { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
