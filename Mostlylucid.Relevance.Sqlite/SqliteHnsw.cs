using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Mostlylucid.Relevance.Sqlite;

// SQLite's extension owns graph creation, persistence, rollback and nearest-neighbour traversal.
// These tables are derived from the ordinary transactional vector rows, not a second .NET index.
internal sealed class SqliteHnsw(SqliteConnection connection, SqliteTransaction transaction, SqliteVectorOptions options)
{
    private static string Table(string scope, string point, string signature) => "rh_" + Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(RelevanceJson.Serialize(new[] { scope, point, signature }))));

    private SqliteCommand Command(string sql, params (string Name, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return command;
    }
    private bool Exists(string name)
    {
        using var command = Command("SELECT 1 FROM sqlite_master WHERE type='table' AND name=@name", ("@name", name));
        return command.ExecuteScalar() is not null;
    }
    public void Delete(string scope, Candidate candidate)
    {
        foreach (var (point, encoded) in candidate.Points)
        {
            var table = Table(scope, point, encoded.Signature);
            if (!Exists(table)) continue;
            using var delete = Command($"DELETE FROM {table} WHERE rowid IN (SELECT rowid FROM relevance_vectors WHERE scope=@scope AND candidate_id=@id AND point=@point)",
                ("@scope", scope), ("@id", candidate.Id), ("@point", point));
            delete.ExecuteNonQuery();
        }
    }
    private string Ensure(string scope, string point, string signature, string metric, int dimensions)
    {
        if (dimensions is < 1 or > 65536) throw new ArgumentException("Invalid native vector dimensions.");
        var table = Table(scope, point, signature);
        if (Exists(table)) return table;
        if (metric is not ("l2" or "cosine")) throw new ArgumentException("Unsupported native metric.");
        using var create = Command($"CREATE VIRTUAL TABLE {table} USING vector(dim={dimensions},type=float4,metric={metric},mode=hnsw,m={options.Connections},ef_construction={options.ConstructionWidth},ef_search={options.SearchWidth})");
        create.ExecuteNonQuery();
        using var populate = Command($"INSERT INTO {table}(rowid,vector) SELECT rowid,vector FROM relevance_vectors WHERE scope=@scope AND point=@point AND signature=@signature",
            ("@scope", scope), ("@point", point), ("@signature", signature));
        populate.ExecuteNonQuery();
        return table;
    }
    public void Upsert(string scope, Candidate candidate)
    {
        foreach (var (point, encoded) in candidate.Points)
        {
            var table = Table(scope, point, encoded.Signature);
            var exists = Exists(table);
            Ensure(scope, point, encoded.Signature, encoded.Metric, encoded.Values.Length);
            if (!exists) continue; // Initial population already includes this transaction's new vector.
            using var insert = Command($"INSERT INTO {table}(rowid,vector) SELECT rowid,vector FROM relevance_vectors WHERE scope=@scope AND candidate_id=@id AND point=@point",
                ("@scope", scope), ("@id", candidate.Id), ("@point", point));
            insert.ExecuteNonQuery();
        }
    }
    public IReadOnlyList<Neighbor> Nearest(string scope, VectorQuery query, int limit)
    {
        var table = Ensure(scope, query.Point, query.Signature, query.Metric, query.Values.Length);
        using var command = Command($"""
            SELECT c.record FROM
              (SELECT rowid,distance FROM {table} WHERE knn_match(distance,@query) LIMIT @limit) nn
            JOIN relevance_vectors v ON v.rowid=nn.rowid
            JOIN relevance_candidates c ON c.scope=v.scope AND c.id=v.candidate_id
            WHERE v.scope=@scope AND v.point=@point AND v.signature=@signature
            """, ("@query", VectorMath.Encode(query.Values)), ("@limit", limit),
            ("@scope", scope), ("@point", query.Point), ("@signature", query.Signature));
        using var reader = command.ExecuteReader();
        List<Neighbor> result = [];
        while (reader.Read())
        {
            var candidate = RelevanceJson.Deserialize<Candidate>(reader.GetString(0))!;
            // Preserve the core's distance units: the extension's L2 is squared L2.
            result.Add(new(candidate, VectorMath.Distance(candidate.Points[query.Point].Values, query.Values, query.Metric)));
        }
        return result.OrderBy(n => n.Distance).ThenBy(n => n.Candidate.Id, StringComparer.Ordinal).ToArray();
    }
}
