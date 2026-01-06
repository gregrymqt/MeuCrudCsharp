using System;

namespace MeuCrudCsharp.Features.Files.DTOs;

// Dtos/BaseUploadDto.cs
public class BaseUploadDto
{
    // O Arquivo em si (obrigatório para todos)
    public IFormFile? File { get; set; }

    // Lógica de Chunking (Universal)
    public bool IsChunk { get; set; } = false;
    public int ChunkIndex { get; set; }
    public int TotalChunks { get; set; }
    public string? FileName { get; set; } // Nome original para remontagem
}
