using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class ContaController : ControllerBase
{
    private readonly IMongoCollection<Conta> _collection;
    private readonly IMongoCollection<Banco> _bancos;

    public ContaController(IMongoDatabase database)
    {
        _collection = database.GetCollection<Conta>("Conta");
        _bancos = database.GetCollection<Banco>("Banco");
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
    public async Task<IActionResult> Post([FromBody] ContaInput input)
    {
        // Verifica banco
        var banco = await _bancos.Find(x => x.CodigoBanco == input.CodigoBanco).FirstOrDefaultAsync();
        if (banco == null) return BadRequest($"Banco com código '{input.CodigoBanco}' não encontrado.");

        // Verifica unicidade do CodigoConta
        var existe = await _collection.Find(x => x.CodigoConta == input.CodigoConta).AnyAsync();
        if (existe) return BadRequest($"CodigoConta '{input.CodigoConta}' já existe.");

        var conta = new Conta
        {
            Nome = input.Nome,
            SaldoInicial = input.SaldoInicial,
            Tipo = input.Tipo,
            BancoId = banco.Id!,
            CodigoConta = input.CodigoConta,
            CreatedAt = input.CreatedAt,
            UpdatedAt = input.UpdatedAt
        };

        await _collection.InsertOneAsync(conta);
        return CreatedAtAction(nameof(GetById), new { id = conta.Id }, conta);
    }

    // 🔹 ATUALIZAR
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] ContaInput input)
    {
        var banco = await _bancos.Find(x => x.CodigoBanco == input.CodigoBanco).FirstOrDefaultAsync();
        if (banco == null) return BadRequest($"Banco com código '{input.CodigoBanco}' não encontrado.");

        // Verifica unicidade do CodigoConta (exceto a própria conta)
        var existe = await _collection.Find(x => x.CodigoConta == input.CodigoConta && x.Id != id).AnyAsync();
        if (existe) return BadRequest($"CodigoConta '{input.CodigoConta}' já existe.");

        var conta = new Conta
        {
            Id = id,
            Nome = input.Nome,
            SaldoInicial = input.SaldoInicial,
            Tipo = input.Tipo,
            BancoId = banco.Id!,
            CodigoConta = input.CodigoConta,
            CreatedAt = input.CreatedAt,
            UpdatedAt = input.UpdatedAt
        };

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

    // 🔹 CRIAR EM LOTE
    [HttpPost("Carga")]
    public async Task<IActionResult> PostCarga([FromBody] List<ContaInput> inputs)
    {
        if (inputs == null || inputs.Count == 0)
            return BadRequest("Lista de contas vazia.");

        var contas = new List<Conta>();
        foreach (var input in inputs)
        {
            var banco = await _bancos.Find(x => x.CodigoBanco == input.CodigoBanco).FirstOrDefaultAsync();
            if (banco == null) return BadRequest($"Banco com código '{input.CodigoBanco}' não encontrado.");

            var existe = await _collection.Find(x => x.CodigoConta == input.CodigoConta).AnyAsync();
            if (existe) return BadRequest($"CodigoConta '{input.CodigoConta}' já existe.");

            contas.Add(new Conta
            {
                Nome = input.Nome,
                SaldoInicial = input.SaldoInicial,
                Tipo = input.Tipo,
                BancoId = banco.Id!,
                CodigoConta = input.CodigoConta,
                CreatedAt = input.CreatedAt,
                UpdatedAt = input.UpdatedAt
            });
        }

        await _collection.InsertManyAsync(contas);
        return Ok(new { Mensagem = $"{contas.Count} contas inseridas com sucesso.", Contas = contas });
    }
}

// DTO
public class ContaInput
{
    public string Nome { get; set; } = "";
    public decimal SaldoInicial { get; set; }
    public TipoConta Tipo { get; set; } = TipoConta.Corrente;
    public string CodigoBanco { get; set; } = "";
    public string CodigoConta { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
