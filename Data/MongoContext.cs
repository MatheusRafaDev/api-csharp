using MongoDB.Driver;

public class MongoContext
{
    private readonly IMongoDatabase _database;

    public MongoContext(IConfiguration configuration)
    {
        var settings = configuration
            .GetSection("MongoDbSettings")
            .Get<MongoDbSettings>();

        var client = new MongoClient(settings.ConnectionString);
        _database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<Receita> Receitas =>
        _database.GetCollection<Receita>("Receitas");

    public IMongoCollection<Lancamento> Lancamentos =>
        _database.GetCollection<Lancamento>("Lancamentos");

    public IMongoCollection<Categoria> Categorias =>
        _database.GetCollection<Categoria>("Categorias");

    public IMongoCollection<Banco> Bancos =>
        _database.GetCollection<Banco>("Bancos");

    public IMongoCollection<Conta> Contas =>
        _database.GetCollection<Conta>("Contas");

    public IMongoCollection<CustosFixos> CustosFixos =>
        _database.GetCollection<CustosFixos>("CustosFixos");

        
}
