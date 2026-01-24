from interfaces.Iproduct_source import IProductSource
from interfaces.Idata_service import IDataService
from interfaces.Iexcel_exporter import IExcelExporter

class ExcelView:
    def __init__(self, source: IProductSource, service: IDataService, exporter: IExcelExporter):
        self.source = source
        self.service = service
        self.exporter = exporter
        
    def run_export(self):
        print("\n" + "="*70)
        print(" INICIANDO EXPORTAÇÃO PARA BI (EXCEL) ".center(70, " "))
        print("="*70)
        
        # 1. Busca os dados (Lógica da API agora na View)
        raw_data = self.source.fetch_products(limit=50, skip=0)
        
        if raw_data:
            # 2. Tratamento
            clean_products, stats = self.service.prepare_products(raw_data)
            
            # 3. Exportação
            sucesso = self.exporter.send_to_excel(clean_products, "relatorio_adm.xlsx")
            
            if sucesso:
                print(f"\n[OK] Arquivo salvo com sucesso!")
                print(f"Sua parceira já pode abrir o arquivo no notebook dela.")
            else:
                print("\n[ERRO] Falha ao gravar o arquivo. Verifique se o Excel está aberto.")
        else:
            print("\n[ERRO] Não foi possível conectar à API.")
        print("="*70 + "\n")