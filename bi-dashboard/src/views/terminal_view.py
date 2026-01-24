from models.cleaned_product_dto import CleanedProductDTO

from interfaces.Iproduct_source import IProductSource
from interfaces.Idata_service import IDataService


class TerminalView:
    def __init__(self, source: IProductSource, service: IDataService):
        self.source = source
        self.service = service

    def run_report(self,total_pages=2):
        print(f"🔍 [DEBUG] Iniciando run_report - Total de páginas: {total_pages}")
        pagina_atual = 1
        skip = 0
        limit = 10

        while pagina_atual <= total_pages:
            print(f"🔍 [DEBUG] Página {pagina_atual} - Buscando produtos (skip={skip}, limit={limit})")
            
            # Busca os dados - retorna List[ProductDTO]
            raw_data = self.source.fetch_products(limit=limit, skip=skip)
            
            print(f"🔍 [DEBUG] Dados recebidos: {type(raw_data)}, Quantidade: {len(raw_data) if raw_data else 0}")
            
            # Corrigido: raw_data é uma lista de ProductDTO, não um dicionário
            if raw_data:
                print(f"🔍 [DEBUG] Processando {len(raw_data)} produtos...")
                clean_products, stats = self.service.prepare_products(raw_data)
                
                print(f"🔍 [DEBUG] Produtos limpos: {len(clean_products)}, Stats: {stats}")
                
                self._display_header(pagina_atual)
                self._show_table(clean_products)
                
                total = self.service.get_dashboard_metrics(clean_products)
                self._display_footer(total["total_value"])

                pagina_atual += 1
                skip += limit
            else:
                print(f"⚠️ [DEBUG] Nenhum dado retornado. Encerrando loop.")
                break
        
        print(f"✅ [DEBUG] Relatório concluído!")

    def _display_header(self, page):
        print("\n" + "="*90)
        print(f" RELATÓRIO DE INVENTÁRIO - PÁGINA {page} ".center(90, "="))
        print("="*90)
        print(f"{'ID':<4} | {'PRODUTO':<25} | {'MARCA':<15} | {'PREÇO':<10} | {'ESTOQUE'}")
        print("-" * 90)
    def _show_table(self, products : list[CleanedProductDTO]):
        for p in products:
            print(f"{p.id:<4} | {p.full_title:<25} | {p.brand[:15]:<15} | ${p.price:<9} | {p.stock} un")

    def _display_footer(self, total_value):
        print("-" * 90)
        print(f"VALOR TOTAL EM ESTOQUE (PÁGINA): ${total_value:,.2f}".rjust(90))
        print("=" * 90)
        