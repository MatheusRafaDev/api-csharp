using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class Categoria
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
