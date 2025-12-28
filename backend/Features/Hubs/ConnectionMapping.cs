using System.Collections.Concurrent;
using System.Linq;

namespace MeuCrudCsharp.Features.Hubs
{
    /// <summary>
    /// Serviço genérico que mantém um mapeamento bidirecional entre uma Chave <T>
    /// e as conexões do SignalR associadas a ela.
    /// </summary>
    public class ConnectionMapping<T>
        where T : notnull
    {
        // Mapeia Chave -> Conjunto de ConnectionIds
        private readonly ConcurrentDictionary<T, HashSet<string>> _keyToConnections = new();

        // Mapeia ConnectionId -> Chave
        private readonly ConcurrentDictionary<string, T> _connectionToKey = new();

        /// <summary>
        /// Adiciona uma associação entre uma chave e uma conexão.
        /// </summary>
        public void Add(T key, string connectionId)
        {
            _connectionToKey.TryAdd(connectionId, key);

            var connections = _keyToConnections.GetOrAdd(key, _ => new HashSet<string>());
            lock (connections)
            {
                connections.Add(connectionId);
            }
        }

        /// <summary>
        /// Obtém a chave associada a um ID de conexão. Essencial para o OnDisconnectedAsync.
        /// </summary>
        public T? GetKey(string connectionId)
        {
            return _connectionToKey.TryGetValue(connectionId, out var key) ? key : default;
        }

        /// <summary>
        /// Obtém todas as conexões associadas a uma chave. Essencial para enviar mensagens.
        /// </summary>
        public IEnumerable<string> GetConnections(T key)
        {
            return _keyToConnections.TryGetValue(key, out var connections)
                ? connections
                : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Remove uma conexão, limpando ambos os mapeamentos.
        /// </summary>
        public void Remove(string connectionId)
        {
            if (!_connectionToKey.TryRemove(connectionId, out var key))
            {
                return;
            }

            if (_keyToConnections.TryGetValue(key, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0)
                    {
                        _keyToConnections.TryRemove(key, out _);
                    }
                }
            }
        }
    }
}
