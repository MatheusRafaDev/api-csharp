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

    // DTO para receber os dados
    public class ReceitaDto
    {
        public string Descricao { get; set; } = "";
        public decimal Valor { get; set; }
        public DateTime Data { get; set; } = DateTime.Now;
        public StatusPagamento Status { get; set; } = StatusPagamento.Pendente;

        // IDs já existentes
        public string CategoriaId { get; set; } = "";
        public string ContaId { get; set; } = "";
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
    public async Task<IActionResult> Post([FromBody] ReceitaDto dto)
    {
        if (string.IsNullOrEmpty(dto.ContaId) || string.IsNullOrEmpty(dto.CategoriaId))
            return BadRequest("ContaId e CategoriaId são obrigatórios.");

        var receita = new Receita
        {
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            Data = dto.Data,
            Status = dto.Status,
            ContaId = dto.ContaId,
            CategoriaId = dto.CategoriaId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _collection.InsertOneAsync(receita);
        return CreatedAtAction(nameof(GetById), new { id = receita.Id }, receita);
    }

    // 🔹 ATUALIZAR
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] ReceitaDto dto)
    {
        if (string.IsNullOrEmpty(dto.ContaId) || string.IsNullOrEmpty(dto.CategoriaId))
            return BadRequest("ContaId e CategoriaId são obrigatórios.");

        var receita = new Receita
        {
            Id = id,
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            Data = dto.Data,
            Status = dto.Status,
            ContaId = dto.ContaId,
            CategoriaId = dto.CategoriaId,
            UpdatedAt = DateTime.Now
        };

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

    // 🔹 CRIAR VÁRIOS (LOTE)
    [HttpPost("carga")]
    public async Task<IActionResult> PostCarga([FromBody] List<ReceitaDto> lista)
    {
        if (lista == null || lista.Count == 0)
            return BadRequest("Lista de receitas vazia.");

        var receitas = lista.Select(dto =>
        {
            if (string.IsNullOrEmpty(dto.ContaId) || string.IsNullOrEmpty(dto.CategoriaId))
                throw new Exception("ContaId e CategoriaId são obrigatórios.");

            return new Receita
            {
                Descricao = dto.Descricao,
                Valor = dto.Valor,
                Data = dto.Data,
                Status = dto.Status,
                ContaId = dto.ContaId,
                CategoriaId = dto.CategoriaId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }).ToList();

        await _collection.InsertManyAsync(receitas);
        return Ok(new { Mensagem = $"{receitas.Count} receitas inseridas com sucesso.", Receitas = receitas });
    }
}
