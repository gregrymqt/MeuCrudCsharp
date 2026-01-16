import requests
import os
import json
from dotenv import load_dotenv

load_dotenv()

class RowsExporter:
    def __init__(self, api_key=None, spreadsheet_id=None, table_id=None, notion_controller=None):
        self.api_key = api_key or os.getenv("ROWS_API_KEY")
        self.spreadsheet_id = spreadsheet_id or os.getenv("ROWS_SPREADSHEET_ID")
        self.table_id = table_id or os.getenv("ROWS_TABLE_ID")
        self.notion = notion_controller
        
    def send_to_rows(self, cleaned_data, dashboard_metrics=None):
        try:
            # 1. Cálculo dinâmico do Range para evitar erro 400
            num_rows = len(cleaned_data) + 2  # +1 cabeçalho, +1 rodapé
            last_row = num_rows
            url_cells = f"https://api.rows.com/v1/spreadsheets/{self.spreadsheet_id}/tables/{self.table_id}/cells/A1:K{last_row}"
            
            headers = ["ID", "Produto", "Categoria", "Marca", "Preço ($)", "Estoque", "Situação", "Patrimônio ($)", "Total Patrimônio", "Alertas Críticos", "Categorias Únicas"]
            matrix = []
            
            # Logs de Depuração Técnica
            print(f"    [LOG] Montando matriz para {len(cleaned_data)} produtos...")
            print(f"    [LOG] Range Alvo: A1:K{last_row} ({num_rows} linhas total)")

            # 1. Cabeçalho
            matrix.append([{"value": str(h)} for h in headers])
            
            # 2. Dados dos Produtos
            for i, p in enumerate(cleaned_data):
                # Verificação de segurança: impede que valores None quebrem o JSON
                product_row = [
                    {"value": str(p.get('id', ''))},
                    {"value": str(p.get('full_title', ''))},
                    {"value": str(p.get('category', ''))},
                    {"value": str(p.get('brand', ''))},
                    {"value": str(p.get('price', 0))},
                    {"value": str(p.get('stock', 0))},
                    {"value": str(p.get('status', ''))},
                    {"value": str(p.get('total_stock_value', 0))},
                    {"value": ""}, 
                    {"value": ""}, 
                    {"value": ""}
                ]
                matrix.append(product_row)

            # 3. Rodapé de Métricas
            if dashboard_metrics:
                metrics_row = [
                    {"value": "RESUMO GERAL"}, 
                    {"value": ""}, {"value": ""}, {"value": ""}, 
                    {"value": ""}, {"value": ""}, {"value": ""}, {"value": ""},
                    {"value": str(dashboard_metrics.get("total_value", 0))},
                    {"value": str(dashboard_metrics.get("critical_alerts", 0))},
                    {"value": str(dashboard_metrics.get("unique_categories", 0))}
                ]
                matrix.append(metrics_row)

            # Validação Final da Matriz antes do envio
            print(f"    [LOG] Matriz final: {len(matrix)} linhas x {len(matrix[0])} colunas")
            
            payload = {"cells": matrix}
            request_headers = {
                "Authorization": f"Bearer {self.api_key}",
                "Content-Type": "application/json",
                "Accept": "application/json"
            }

            # Envio
            response = requests.post(url_cells, json=payload, headers=request_headers)
            
            if response.status_code in [200, 201, 202]:
                print(f"✅ SUCESSO: Dashboard Greg Company atualizado via POST!")
                if self.notion:
                    self.notion.update_status("Sincronização Rows.com realizada com sucesso!", is_ok=True)
                return True
            else:
                # Log detalhado do erro da API
                print(f"❌ ERRO API ROWS ({response.status_code})")
                print(f"   Mensagem: {response.text}")
                print(f"   Dica: Verifique se o range A1:K{last_row} existe na planilha.")
                return False
                
        except Exception as e:
            if self.notion:
                self.notion.update_status(f"Erro Interno: {str(e)[:40]}", is_ok=False)
            print(f"💥 Falha na execução do RowsExporter: {e}")
            return False