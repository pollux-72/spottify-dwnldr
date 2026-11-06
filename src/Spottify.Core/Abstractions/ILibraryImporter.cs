namespace Spottify.Core.Abstractions;

/// <summary>Moves finished downloads into the music library with correct tags. Detailed by TASK-12.</summary>
public interface ILibraryImporter
{
    Task<ImportResult> ImportAsync(string downloadPath, AlbumDetails album, CancellationToken cancellationToken);
}

/// <param name="LibraryPath">Album folder inside the library when the import succeeded.</param>
public sealed record ImportResult(bool Success, string? LibraryPath, string? Error);
