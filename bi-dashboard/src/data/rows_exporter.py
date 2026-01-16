import requests
import os
from dotenv import load_dotenv

load_dotenv()
    
class RowsExporter:
    def __init__(self, api_key=None, spreadsheet_id=None, table_id=None):
        self.api_key = api_key or os.getenv("ROWS_API_KEY")
        self.spreadsheet_id = spreadsheet_id or os.getenv("ROWS_SPREADSHEET_ID")
        self.table_id = table_id or os.getenv("ROWS_TABLE_ID")
        
    def send_to_rows(self, cleaned_data, dashboard_metrics=None):
        try:
            # A1:K52 permite 11 colunas (A até K) e 52 linhas (cabeçalho + 50 prod + 1 métrica)
            url_cells = f"https://api.rows.com/v1/spreadsheets/{self.spreadsheet_id}/tables/{self.table_id}/cells/A1:K52"
            
            headers = ["ID", "Produto", "Categoria", "Marca", "Preço ($)", "Estoque", "Situação", "Patrimônio ($)", "Total Patrimônio", "Alertas Críticos", "Categorias Únicas"]
            matrix = []
            
            # 1. Cabeçalho
            matrix.append([{"value": str(h)} for h in headers])
            
            # 2. Produtos (Preenchemos as colunas de métricas com vazio para não desalinharem)
            for p in cleaned_data:
                product_row = [
                    {"value": str(p['id'])},
                    {"value": str(p['full_title'])},
                    {"value": str(p['category'])},
                    {"value": str(p['brand'])},
                    {"value": str(p['price'])},
                    {"value": str(p['stock'])},
                    {"value": str(p['status'])},
                    {"value": str(p['total_stock_value'])},
                    {"value": ""}, # Espaço vazio para a coluna 'Total Patrimônio'
                    {"value": ""}, # Espaço vazio para a coluna 'Alertas Críticos'
                    {"value": ""}  # Espaço vazio para a coluna 'Categorias Únicas'
                ]
                matrix.append(product_row)

            # 3. Linha de Métricas (Fica no final da tabela)
            if dashboard_metrics:
                # Alinhamos os valores nas colunas I, J e K (9, 10 e 11)
                metrics_row = [
                    {"value": "RESUMO"}, # A
                    {"value": ""},        # B
                    {"value": ""},        # C
                    {"value": ""},        # D
                    {"value": ""},        # E
                    {"value": ""},        # F
                    {"value": ""},        # G
                    {"value": ""},        # H
                    {"value": str(dashboard_metrics["total_value"])},      # I (Total Patrimônio)
                    {"value": str(dashboard_metrics["critical_alerts"])},   # J (Alertas)
                    {"value": str(dashboard_metrics["unique_categories"])}  # K (Categorias)
                ]
                matrix.append(metrics_row)

            payload = {"cells": matrix}
            request_headers = {
                "Authorization": f"Bearer {self.api_key}",
                "Content-Type": "application/json",
                "Accept": "application/json"
            }

            # Dica: Use PUT para substituir o conteúdo do intervalo inteiro
            response = requests.put(url_cells, json=payload, headers=request_headers)
            
            if response.status_code in [200, 201, 202]:
                print(f"✅ SUCESSO: Dashboard Greg Company atualizado (A1:K52)!")
                return True
            else:
                print(f"❌ ERRO API: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ Erro no processamento: {e}")
            return False