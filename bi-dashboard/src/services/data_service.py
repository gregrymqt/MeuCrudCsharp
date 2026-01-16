

class DataService:
    def prepare_products(self, raw_products):
        cleaned_data = []
        for item in raw_products:
            title = item.get('title') or "Produto Sem Nome"
            brand = item.get('brand') or "S/ Marca"
            category = item.get('category', 'Geral').capitalize()
            stock = item.get('stock', 0)
            price = item.get('price', 0.0)

            # Lógica de Administração: Alerta de estoque baixo
            # Se o estoque for menor que 15, marcamos para reposição
            match stock:
                case 0:
                    status = "🔴 ESGOTADO"
                case s if s < 10:
                    status = "⚠️ CRÍTICO"
                case s if s < 20:
                    status = "🟡 REPOR"
                case _:
                    status = "🟢 OK"
            
            cleaned_data.append({
                "id": item.get('id'),
                "display_title": (title[:22] + '..') if len(title) > 22 else title,
                "full_title": title,
                "brand": brand,
                "category": category, # Novo campo
                "price": price,
                "stock": stock,
                "status": status,     # Novo campo
                "total_stock_value": price * stock
            })
        return cleaned_data

    def get_dashboard_metrics(self, cleaned_products):
        # 1. =SUM('Greg Company'!H:H) -> Patrimônio Total
        total_patrimonio = sum(p['total_stock_value'] for p in cleaned_products)

        # 2. =COUNTIF('Greg Company'!G:G,"*⚠️*") -> Contagem de itens CRÍTICOS
        # Procuramos o emoji ou a palavra "CRÍTICO" dentro da string status
        count_critico = sum(1 for p in cleaned_products if "⚠️" in p['status'])

        # 3. =COUNTUNIQUE('Greg Company'!C:C) -> Quantidade de categorias únicas
        # Usamos o 'set' que automaticamente remove duplicatas
        categorias_unicas = len(set(p['category'] for p in cleaned_products))

        return {
            "total_value": total_patrimonio,
            "critical_alerts": count_critico,
            "unique_categories": categorias_unicas
        }