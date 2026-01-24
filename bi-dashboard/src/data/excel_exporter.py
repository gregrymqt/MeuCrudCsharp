import pandas as pd
import os
from dataclasses import asdict
from typing import List
from dotenv import load_dotenv
from interfaces.Iexcel_exporter import IExcelExporter
from models.cleaned_product_dto import CleanedProductDTO

load_dotenv()

class ExcelExporter(IExcelExporter):
    def __init__(self, directory: str = None):
        # Utiliza o caminho da FATEC configurado no .env ou um padrão
        self.directory = directory or os.getenv("caminho_fatec", "output")
        if not os.path.exists(self.directory):
            os.makedirs(self.directory)

    def send_to_excel(self, products: List[CleanedProductDTO], filename: str) -> bool:
        """Transforma a lista de DTOs em uma planilha configurada para BI"""
        try:
            # Converte a lista de objetos CleanedProductDTO em uma lista de dicionários
            data_dicts = [asdict(p) for p in products]
            df = pd.DataFrame(data_dicts)

            # Mapeamento de colunas para o relatório administrativo do Greg Company
            cols_to_export = [
                'id', 'full_title', 'category', 'brand', 
                'price', 'stock', 'status', 'total_stock_value'
            ]
            
            # Renomeação para nomes amigáveis no Excel
            df_final = df[cols_to_export].rename(columns={
                'id': 'ID',
                'full_title': 'Produto',
                'category': 'Categoria',
                'brand': 'Marca',
                'price': 'Preço Unitário ($)',
                'stock': 'Estoque (Qtd)',
                'status': 'Situação',
                'total_stock_value': 'Patrimônio em Estoque'
            })

            path = os.path.join(self.directory, filename)
            
            # Salvando o arquivo no diretório configurado
            df_final.to_excel(path, index=False)
            print(f"✅ Relatório Excel gerado com sucesso em: {path}")
            return True
            
        except Exception as e:
            print(f"❌ Erro ao salvar o arquivo Excel: {e}")
            return False