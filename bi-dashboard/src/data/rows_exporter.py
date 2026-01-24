import requests
import os
from typing import List, Dict
from dotenv import load_dotenv
from interfaces.Iproduct_exporter import IProductExporter
from models.cleaned_product_dto import CleanedProductDTO
from controllers.notion_controller import NotionController

load_dotenv()

class RowsExporter(IProductExporter):
    def __init__(self, notion_controller : NotionController):
        self.api_key = os.getenv("ROWS_API_KEY")
        self.spreadsheet_id = os.getenv("ROWS_SPREADSHEET_ID")
        self.table_id = os.getenv("ROWS_TABLE_ID")
        self.notion = notion_controller
        
    def send_to_rows(self, products: List[CleanedProductDTO], metrics: Dict[str, float]) -> bool:
        try:
            # 1. Configuração do Range Dinâmico
            num_rows = len(products) + 2 # +1 cabeçalho, +1 rodapé
            url_cells = f"https://api.rows.com/v1/spreadsheets/{self.spreadsheet_id}/tables/{self.table_id}/cells/A1:K{num_rows}"
            
            headers = ["ID", "Produto", "Categoria", "Marca", "Preço ($)", "Estoque", "Situação", "Patrimônio ($)", "Total Patrimônio", "Alertas Críticos", "Categorias Únicas"]
            matrix = []
            
            # Cabeçalho
            matrix.append([{"value": str(h)} for h in headers])
            
            # 2. Dados dos Produtos (Usando os atributos do DTO)
            for p in products:
                product_row = [
                    {"value": str(p.id)},
                    {"value": p.full_title},
                    {"value": p.category},
                    {"value": p.brand},
                    {"value": str(p.price)},
                    {"value": str(p.stock)},
                    {"value": str(p.status)}, # O Enum ProductStatus é convertido em string
                    {"value": str(p.total_stock_value)},
                    {"value": ""}, {"value": ""}, {"value": ""}
                ]
                matrix.append(product_row)

            # 3. Rodapé de Métricas
            metrics_row = [
                {"value": "RESUMO GERAL"}, 
                {"value": ""}, {"value": ""}, {"value": ""}, 
                {"value": ""}, {"value": ""}, {"value": ""}, {"value": ""},
                {"value": str(metrics.get("total_value", 0))},
                {"value": str(metrics.get("critical_alerts", 0))},
                {"value": str(metrics.get("unique_categories", 0))}
            ]
            matrix.append(metrics_row)

            # 4. Envio para a API
            payload = {"cells": matrix}
            request_headers = {
                "Authorization": f"Bearer {self.api_key}",
                "Content-Type": "application/json",
                "Accept": "application/json"
            }

            response = requests.post(url_cells, json=payload, headers=request_headers)
            
            if response.status_code in [200, 201, 202]:
                print(f"✅ SUCESSO: Dashboard Greg Company atualizado via POST!")
                if self.notion:
                    self.notion.update_status("Sincronização Rows.com realizada com sucesso!", is_ok=True)
                return True
            else:
                print(f"❌ ERRO API ROWS ({response.status_code}): {response.text}")
                return False
                
        except Exception as e:
            if self.notion:
                self.notion.update_status(f"Erro Interno no RowsExporter: {str(e)[:30]}", is_ok=False)
            print(f"💥 Falha na execução do RowsExporter: {e}")
            return False