using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


public class Receita
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Descricao { get; set; } = "";

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Valor { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime Data { get; set; } = DateTime.Now;

    public StatusPagamento Status { get; set; } = StatusPagamento.Pendente;

    // Referência ObjectId
    public string CategoriaId { get; set; } = "";
    public string ContaId { get; set; } = "";

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
