from mcp.server.fastmcp import FastMCP
import os

# Inicializa o servidor focado no contexto do Greg Company
mcp = FastMCP("GregCompany-Context")

# Caminho base do seu projeto (ajuste se necessário)
PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SYSTEM_APP_PATH = os.path.join(PROJECT_ROOT, "system-app")

@mcp.tool()
def explicar_arquitetura():
    """Retorna as regras de arquitetura e padrões do projeto."""
    try:
        instructions_path = os.path.join(PROJECT_ROOT, '.github', 'copilot-instructions.md')
        with open(instructions_path, 'r', encoding='utf-8') as f:
            return f.read()
    except Exception as e:
        return f"Erro ao ler as instruções de arquitetura: {e}"

@mcp.tool()
def mapear_estrutura_system():
    """Lista as pastas atuais do sistema para evitar criação de arquivos em locais errados."""
    try:
        estrutura = []
        for root, dirs, files in os.walk(SYSTEM_APP_PATH):
            # Ignora pastas pesadas
            dirs[:] = [d for d in dirs if d not in ['node_modules', 'bin', 'obj', '.venv']]
            nivel = root.replace(SYSTEM_APP_PATH, '').count(os.sep)
            indent = ' ' * 4 * nivel
            estrutura.append(f"{indent}{os.path.basename(root)}/")
        return "\n".join(estrutura)
    except Exception as e:
        return f"Erro ao ler pastas: {e}"

if __name__ == "__main__":

    mcp.run()






