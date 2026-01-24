from dataclasses import dataclass
from enums.product_status import ProductStatus

@dataclass(frozen=True)
class CleanedProductDTO:
    """DTO para dados processados e prontos para o Dashboard"""
    id: int
    display_title: str
    full_title: str
    brand: str
    category: str
    price: float
    stock: int
    status: ProductStatus
    total_stock_value: float