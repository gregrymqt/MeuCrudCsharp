import pandas as pd
import os
from dotenv import load_dotenv

load_dotenv()

class DataExporter:
    def __init__(self, directory=None):
        # Se um diretório não for passado, usa o valor de 'caminho_fatec' do .env,
        # ou 'output' como padrão final.
        self.directory = directory or os.getenv("caminho_fatec", "output")
        if not os.path.exists(self.directory):
            os.makedirs(self.directory)

    def save_to_excel(self, cleaned_data, filename):
        """Transforma a lista limpa em uma planilha Excel configurada para BI."""
        try:
            df = pd.DataFrame(cleaned_data)

            # Selecionamos as colunas na ordem ideal para análise administrativa
            # Note que incluímos 'category' e 'status'
            cols_to_export = [
                'id', 'full_title', 'category', 'brand', 
                'price', 'stock', 'status', 'total_stock_value'
            ]
            
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
            
            # Salvando no caminho configurado
            df_final.to_excel(path, index=False)
            return True
        except Exception as e:
            print(f"Erro ao salvar o arquivo Excel: {e}")
            return False