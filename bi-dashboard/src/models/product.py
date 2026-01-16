class Product:
    def __init__(self, id, title, price, stock, brand, category):
        self.id = id
        self.title = title
        self.price = price
        self.stock = stock
        self.brand = brand
        self.category = category

    @staticmethod
    def from_json(data):
        # Deserialização: Transforma o dicionário JSON em um objeto Product
        return Product(
            data.get('id'), 
            data.get('title'), 
            data.get('price'), 
            data.get('stock'), 
            data.get('brand'), 
            data.get('category')
        )