using System;

namespace MeuCrudCsharp.Models;

public class EntityFile
    {
        public int Id { get; set; }
        public string NomeArquivo { get; set; }      // Ex: "guid_relatorio.pdf"
        public string CaminhoRelativo { get; set; }  // Ex: "uploads/Financeiro/guid_relatorio.pdf"
        public string ContentType { get; set; }      // Ex: "application/pdf", "image/png"
        public string FeatureCategoria { get; set; } // Ex: "Financeiro", "Perfil", "VideosAula"
        public long TamanhoBytes { get; set; }       // Útil para validações
    }
