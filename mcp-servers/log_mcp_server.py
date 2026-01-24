from mcp.server.fastmcp import FastMCP
import docker # Biblioteca para conversar com o Docker Desktop
import os

mcp = FastMCP("GregCompany-Logs")
client = docker.from_env() # Conecta no seu Docker Desktop automaticamente

@mcp.tool()
def ler_logs_servico(nome_servico: str, linhas: int = 20):
    """
    Lê os últimos logs de um container específico.
    Exemplos de nome_servico: 'mssql-db', 'mongodb-store', 'redis-cache', 'backend-api'
    """
    try:
        # Busca o container pelo nome definido no seu docker-compose
        container = client.containers.get(nome_servico)
        # Pega os logs brutos e decodifica para texto
        logs = container.logs(tail=linhas).decode('utf-8')
        return f"--- Últimos {linhas} logs de {nome_servico} ---\n{logs}"
    except Exception as e:
        return f"Erro ao acessar container {nome_servico}: {e}"

@mcp.tool()
def analisar_saude_infra():
    """Verifica se todos os bancos e o cache estão rodando."""
    servicos = ['mssql-db', 'mongodb-store', 'redis-cache']
    status_geral = ""
    for s in servicos:
        try:
            container = client.containers.get(s)
            status_geral += f"Serviço {s}: {container.status}\n"
        except:
            status_geral += f"Serviço {s}: NÃO ENCONTRADO\n"
    return status_geral

if __name__ == "__main__":
    mcp.run()