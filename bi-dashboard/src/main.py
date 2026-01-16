from src.controllers.api_controller import APIController
from src.controllers.notion_controller import NotionController
from src.services.data_service import DataService
from src.views.terminal_view import TerminalView
from src.views.excel_view import ExcelView
from src.views.rows_view import RowsView      # Nova View
from src.data.data_exporter import DataExporter
from src.data.rows_exporter import RowsExporter

def main():
    # --- INICIALIZAÇÃO ---
    api = APIController()
    service = DataService()
    t_view = TerminalView()
    e_view = ExcelView()
    r_view = RowsView() # Instância da nova View
    excel_exp = DataExporter()
    notion = NotionController()
    rows_exp = RowsExporter(notion_controller=notion)

    print(f"\n=== SISTEMA GREG COMPANY | GESTÃO ADMINISTRATIVA ===")
    print("1. Relatório no Terminal (Páginas 1 e 2)")
    print("2. Gerar Planilha Excel (OneDrive Fatec)")
    print("3. Sincronizar Dashboard Notion (Rows.com)")
    print("4. Testar Conexão API Notion (Apenas Status)")
    print("0. Sair")
    
    escolha = input("\nO que deseja fazer? ")

    if escolha == "1":
        t_view.run_report(api, service)
    elif escolha == "2":
        # Orquestra o Excel através da ExcelView
        e_view.run_export(api, service, excel_exp)
    elif escolha == "3":
        # Orquestra o Rows através da RowsView
        r_view.run_rows_sync(api, service, rows_exp)
    elif escolha == "4":
        print("Enviando sinal de teste para o Notion...")
        sucesso = notion.update_status("Teste de API realizado com sucesso!", is_ok=True)
        if sucesso:
            print("✅ Verifique seu Notion, o status deve ter mudado!")
        else:
            print("❌ Falha no teste. Verifique seu .env e o Block ID.")    
    elif escolha == "0":
        print("Saindo...")

if __name__ == "__main__":
    main()