from typing import List
import requests
from models.product_dto import ProductDTO
from interfaces.Iproduct_source import IProductSource

class DummyJsonController(IProductSource):
    BASE_URL = "https://dummyjson.com/products"

    def fetch_products(self, limit=10, skip=0) -> List[ProductDTO]:
        params = {"limit": limit, "skip": skip}
        try:
            response = requests.get(self.BASE_URL, params=params)
            response.raise_for_status()

            # O DummyJSON retorna um objeto: {"products": [...], "total": 100...}
            data = response.json()
            raw_list = data.get("products", [])

            # 2. Desserialização Manual (Transformando Dict em Objeto)
            # Para cada dicionário na lista, criamos uma instância de ProductDTO
            return [ProductDTO(**item) for item in raw_list]

        except requests.exceptions.RequestException as e:
            print(f"❌ Erro ao acessar API: {e}")
            return []