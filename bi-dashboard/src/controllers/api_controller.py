
import requests

class APIController:
    BASE_URL = "https://dummyjson.com/products"

    def get_products(self, limit=10, skip=0):
        params = {"limit": limit, "skip": skip}
        try:
            response = requests.get(self.BASE_URL, params=params)
            response.raise_for_status() # Verifica se houve erro na requisição
            return response.json()
        except requests.exceptions.RequestException as e:
            print(f"Erro ao acessar API: {e}")
            return None