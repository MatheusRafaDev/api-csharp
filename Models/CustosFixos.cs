using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class CustosFixos
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Descricao { get; set; } = "";

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Valor { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime Vencimento { get; set; }
    // Referência ObjectId
    public string CategoriaId { get; set; } = "";

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
