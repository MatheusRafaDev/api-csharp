using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public enum TipoConta
{
    Corrente,
    Poupanca
}

public class Banco
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Nome { get; set; } = "";
    public string Agencia { get; set; } = "";
    public string CodigoBanco { get; set; } = "";
    public TipoConta TipoConta { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
