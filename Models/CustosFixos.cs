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

    // Referência via ObjectId
    public string CategoriaId { get; set; } = "";
    public string ContaId { get; set; } = "";

    // Código legível para uso rápido
    public string CategoriaCodigo { get; set; } = "";
    public string ContaCodigo { get; set; } = "";

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
