namespace MeuCrudCsharp.Documents.Interfaces;

public interface IMongoDocument
{
    static abstract string CollectionName { get; }
}
