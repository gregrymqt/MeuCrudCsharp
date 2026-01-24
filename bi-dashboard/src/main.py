from controllers.dummy_json_controller import DummyJsonController
from controllers.notion_controller import NotionController
from services.data_service import DataService
from views.terminal_view import TerminalView
from views.excel_view import ExcelView
from views.rows_view import RowsView
from data.excel_exporter import ExcelExporter
from data.rows_exporter import RowsExporter

def main():
    # --- 1. INSTANCIAÇÃO (CLASSES CONCRETAS) ---
    # Criamos as peças do "quebra-cabeça"
    api = DummyJsonController()
    service = DataService()
    notion = NotionController()
    
    # Exporters (Implementam IProductExporter e IExcelExporter)
    excel_exp = ExcelExporter()
    rows_exp = RowsExporter(notion_controller=notion)

    # --- 2. INJEÇÃO DE DEPENDÊNCIA (VIEWS) ---
    # As Views agora recebem as interfaces no construtor
    # Isso elimina a necessidade de passar 'api' ou 'service' em cada método
    t_view = TerminalView(source=api, service=service)
    e_view = ExcelView(source=api, service=service, exporter=excel_exp)
    r_view = RowsView(source=api, service=service, exporter=rows_exp)

    # --- 3. INTERFACE DE USUÁRIO ---
    print(f"\n" + "="*50)
    print(f" SISTEMA GREG COMPANY | GESTAO ADMINISTRATIVA ".center(50, "="))
    print("="*50)
    print("1. Relatório no Terminal (Páginas 1 e 2)")
    print("2. Gerar Planilha Excel (OneDrive Fatec)")
    print("3. Sincronizar Dashboard Notion (Rows.com)")
    print("4. Testar Conexão API Notion (Apenas Status)")
    print("0. Sair")
    
    while True:
        escolha = input("\nO que deseja fazer? ")

        if escolha == "1":
            t_view.run_report() # Método agora é limpo, sem parâmetros
        elif escolha == "2":
            e_view.run_export() # As dependências já estão "dentro" da view
        elif escolha == "3":
            r_view.run_rows_sync()
        elif escolha == "4":
            print("Enviando sinal de teste para o Notion...")
            notion.update_status("Teste de API realizado com sucesso!", is_ok=True)
        elif escolha == "0":
            print("Encerrando sistema...")
            break
        else:
            print("❌ Opção inválida!")

if __name__ == "__main__":
    main()