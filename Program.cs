using Microsoft.Data.SqlClient;

var cs = "Server=localhost;Database=EduVietRAG;User Id=sa;Password=12345;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;";
await using var conn = new SqlConnection(cs);
await conn.OpenAsync();

await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT Status, COUNT(*) AS Count FROM rag_documents GROUP BY Status ORDER BY Status";
    await using var reader = await cmd.ExecuteReaderAsync();
    Console.WriteLine("Status counts:");
    while (await reader.ReadAsync()) Console.WriteLine($"{reader.GetString(0)}\t{reader.GetInt32(1)}");
}

await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = @"SELECT TOP 20 CONVERT(varchar(36), Id), FileName, Subject, Chapter, Status, ChunkCount, UploadedAt, IndexedAt, LEFT(IndexError, 500)
FROM rag_documents
ORDER BY UploadedAt DESC";
    await using var reader = await cmd.ExecuteReaderAsync();
    Console.WriteLine("\nRecent documents:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"{reader.GetString(0)} | {reader.GetString(1)} | {reader.GetString(4)} | chunks={reader.GetInt32(5)} | uploaded={reader.GetDateTimeOffset(6):u} | indexed={(reader.IsDBNull(7) ? "NULL" : reader.GetDateTimeOffset(7).ToString("u"))} | error={(reader.IsDBNull(8) ? "" : reader.GetString(8))}");
    }
}
