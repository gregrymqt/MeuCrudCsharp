from enum import StrEnum

class ProductStatus(StrEnum):
    OK = "🟢 OK"
    REPOR = "🟡 REPOR"
    CRITICO = "⚠️ CRÍTICO"
    ESGOTADO = "🔴 ESGOTADO"