class TerminalView:
    def run_report(self, api_controller, data_service, total_pages=2):
        pagina_atual = 1
        skip = 0
        limit = 10

        while pagina_atual <= total_pages:
            # Busca os dados
            raw_data = api_controller.get_products(limit=limit, skip=skip)
            
            if raw_data and "products" in raw_data:
                clean_products = data_service.prepare_products(raw_data["products"])
                
                self._display_header(pagina_atual)
                self._show_table(clean_products)
                
                total = data_service.get_dashboard_metrics(clean_products)
                self._display_footer(total["total_value"])

                pagina_atual += 1
                skip += limit
            else:
                break

    def _display_header(self, page):
        print("\n" + "="*70)
        print(f" RELATÓRIO DE INVENTÁRIO - PÁGINA {page} ".center(70, "="))
        print("="*70)
        print(f"{'ID':<4} | {'PRODUTO':<25} | {'MARCA':<15} | {'PREÇO':<10} | {'ESTOQUE'}")
        print("-" * 70)

    def _show_table(self, products):
        for p in products:
            print(f"{p['id']:<4} | {p['display_title']:<25} | {p['brand'][:15]:<15} | ${p['price']:<9} | {p['stock']} un")

    def _display_footer(self, total_value):
        print("-" * 70)
        print(f"VALOR TOTAL EM ESTOQUE (PÁGINA): ${total_value:,.2f}".rjust(70))
        print("=" * 70)