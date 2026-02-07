using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class LancamentosController : ControllerBase
{
    private readonly IMongoCollection<Lancamento> _collection;
    private readonly IMongoCollection<Conta> _contas;
    private readonly IMongoCollection<Categoria> _categorias;

    public LancamentosController(IMongoDatabase database)
    {
        _collection = database.GetCollection<Lancamento>("Lancamento");
        _contas = database.GetCollection<Conta>("Conta");
        _categorias = database.GetCollection<Categoria>("Categoria");
    }

    // DTO de entrada
    public class LancamentoDto
    {
        public string Descricao { get; set; } = "";
        public decimal Valor { get; set; }
        public DateTime Data { get; set; } = DateTime.Now;
        public TipoLancamento Tipo { get; set; }
        public StatusPagamento Status { get; set; } = StatusPagamento.Pendente;

        public string CategoriaCodigo { get; set; } = "";
        public string CodigoConta { get; set; } = "";
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
    public async Task<IActionResult> Post([FromBody] LancamentoDto dto)
    {
        var categoria = await _categorias.Find(x => x.CodigoCategoria == dto.CategoriaCodigo).FirstOrDefaultAsync();
        if (categoria == null) return BadRequest($"Categoria com código '{dto.CategoriaCodigo}' não encontrada");

        var conta = await _contas.Find(x => x.CodigoConta == dto.CodigoConta).FirstOrDefaultAsync();
        if (conta == null) return BadRequest($"Conta com código '{dto.CodigoConta}' não encontrada");

        var lancamento = new Lancamento
        {
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            Data = dto.Data,
            Tipo = dto.Tipo,
            Status = dto.Status,
            CategoriaId = categoria.Id!,
            ContaId = conta.Id!,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _collection.InsertOneAsync(lancamento);
        return CreatedAtAction(nameof(GetById), new { id = lancamento.Id }, lancamento);
    }

    // 🔹 ATUALIZAR
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] LancamentoDto dto)
    {
        var categoria = await _categorias.Find(x => x.CodigoCategoria == dto.CategoriaCodigo).FirstOrDefaultAsync();
        if (categoria == null) return BadRequest($"Categoria com código '{dto.CategoriaCodigo}' não encontrada");

        var conta = await _contas.Find(x => x.CodigoConta == dto.CodigoConta).FirstOrDefaultAsync();
        if (conta == null) return BadRequest($"Conta com código '{dto.CodigoConta}' não encontrada");

        var lancamento = new Lancamento
        {
            Id = id,
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            Data = dto.Data,
            Tipo = dto.Tipo,
            Status = dto.Status,
            CategoriaId = categoria.Id!,
            ContaId = conta.Id!,
            UpdatedAt = DateTime.Now
        };

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
            .Where(x => x.Tipo == TipoLancamento.Entrada)
            .Sum(x => x.Valor);

        var saidas = lancamentos
            .Where(x => x.Tipo == TipoLancamento.Saida)
            .Sum(x => x.Valor);

        return Ok(new
        {
            Entradas = entradas,
            Saidas = saidas,
            Saldo = entradas - saidas
        });
    }

    // 🔹 CRIAR VÁRIOS (LOTE) usando códigos
    [HttpPost("carga")]
    public async Task<IActionResult> PostCarga([FromBody] List<LancamentoDto> lista)
    {
        if (lista == null || lista.Count == 0)
            return BadRequest("Lista de lançamentos vazia.");

        var lancamentos = new List<Lancamento>();

        foreach (var dto in lista)
        {
            var categoria = await _categorias.Find(x => x.CodigoCategoria == dto.CategoriaCodigo).FirstOrDefaultAsync();
            if (categoria == null) return BadRequest($"Categoria com código '{dto.CategoriaCodigo}' não encontrada");

            var conta = await _contas.Find(x => x.CodigoConta == dto.CodigoConta).FirstOrDefaultAsync();
            if (conta == null) return BadRequest($"Conta com código '{dto.CodigoConta}' não encontrada");

            lancamentos.Add(new Lancamento
            {
                Descricao = dto.Descricao,
                Valor = dto.Valor,
                Data = dto.Data,
                Tipo = dto.Tipo,
                Status = dto.Status,
                CategoriaId = categoria.Id!,
                ContaId = conta.Id!,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        await _collection.InsertManyAsync(lancamentos);
        return Ok(new { Mensagem = $"{lancamentos.Count} lançamentos inseridos com sucesso.", Lancamentos = lancamentos });
    }
}
