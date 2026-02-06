using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
[JsonConverter(typeof(TipoContaConverter))]
public enum TipoConta
{
    Corrente,  // C
    Poupanca   // P
}

// Conversor customizado para aceitar "C" e "P" no JSON
public class TipoContaConverter : JsonConverter<TipoConta>
{
    public override TipoConta Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "C" => TipoConta.Corrente,
            "P" => TipoConta.Poupanca,
            _ => throw new JsonException($"Valor inválido para TipoConta: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, TipoConta value, JsonSerializerOptions options)
    {
        var code = value switch
        {
            TipoConta.Corrente => "C",
            TipoConta.Poupanca => "P",
            _ => throw new JsonException($"TipoConta desconhecido: {value}")
        };
        writer.WriteStringValue(code);
    }
}

public class Conta
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Nome { get; set; } = "";

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal SaldoInicial { get; set; }
    public TipoConta Tipo { get; set; } = TipoConta.Corrente;
    public string BancoId { get; set; } = "";
    public string ContaCodigo { get; set; } = "";

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
