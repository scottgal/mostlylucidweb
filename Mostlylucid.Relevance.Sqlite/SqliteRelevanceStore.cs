using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Mostlylucid.Relevance.Sqlite;

/// <summary>Serialized SQLite transactions over ordinary metadata/vector tables with exact filtered retrieval.</summary>
public sealed class SqliteRelevanceStore : IRelevanceStore
{
    private readonly SqliteConnection connection;
    private readonly Dictionary<string, FileStream> leases = [];
    private readonly string? leaseRoot;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly bool native;
    private readonly SqliteVectorOptions? vectorOptions;
    private readonly System.Text.Json.Serialization.JsonSerializerContext? recordTypes;
    /// <summary>Describes the active exact-distance implementation; neither mode is an approximate index.</summary>
    public string SearchMode => vectorOptions is not null ? "SQLite HNSW; filtered queries use exact SQL" : native ? "sqlite-vec exact SQL" : "managed exact SQL function";

    /// <summary>Opens a SQLite database with WAL/FULL durability, creating the schema if needed.</summary>
    /// <param name="databasePath">Local database path or :memory:; parent directories are not created.</param>
    /// <param name="vectorExtensionPath">Optional sqlite-vec native binary loaded with sqlite3_vec_init; failures propagate.</param>
    /// <param name="vectorOptions">Optional SQLite-owned HNSW index; cannot be combined with sqlite-vec.</param>
    /// <param name="recordTypes">Optional generated metadata for application-owned provider records.</param>
    /// <remarks>No extension means managed exact distance functions. Connections are not pooled.</remarks>
    public SqliteRelevanceStore(string databasePath, string? vectorExtensionPath = null, System.Text.Json.Serialization.JsonSerializerContext? recordTypes = null, SqliteVectorOptions? vectorOptions = null)
    {
        if (vectorExtensionPath is not null && vectorOptions is not null) throw new ArgumentException("Choose one SQLite vector extension.");
        vectorOptions?.Validate();
        this.vectorOptions = vectorOptions;
        leaseRoot = vectorOptions is null || databasePath == ":memory:" ? null : Path.GetFullPath(databasePath);
        this.recordTypes = recordTypes;
        connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false }.ToString());
        try
        {
            connection.Open();
            connection.CreateCollation("RELEVANCE_ORDINAL", string.CompareOrdinal);
            if (vectorExtensionPath is not null)
            {
                connection.LoadExtension(Path.GetFullPath(vectorExtensionPath), "sqlite3_vec_init");
                using var verify = connection.CreateCommand();
                verify.CommandText = "SELECT vec_version()";
                verify.ExecuteScalar();
                native = true;
            }
            if (vectorOptions is not null)
                connection.LoadExtension(Path.GetFullPath(vectorOptions.ExtensionPath), "sqlite3_sqlitevectorrs_init");
            connection.CreateFunction<byte[], byte[], string, double>("relevance_distance",
                (a, b, metric) => VectorMath.Distance(VectorMath.Decode(a), VectorMath.Decode(b), metric), isDeterministic: true);
            using var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA journal_mode=WAL;
                PRAGMA synchronous=FULL;
                PRAGMA foreign_keys=ON;
                PRAGMA busy_timeout=5000;
                CREATE TABLE IF NOT EXISTS relevance_schema(scope TEXT PRIMARY KEY, fingerprint TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS relevance_records(
                    scope TEXT NOT NULL, kind TEXT NOT NULL, id TEXT NOT NULL,
                    payload TEXT NOT NULL, expires INTEGER NOT NULL,
                    PRIMARY KEY(scope, kind, id));
                CREATE INDEX IF NOT EXISTS relevance_expiry ON relevance_records(expires);
                CREATE TABLE IF NOT EXISTS relevance_candidates(
                    scope TEXT NOT NULL, id TEXT NOT NULL, payload TEXT NOT NULL, record TEXT NOT NULL,
                    PRIMARY KEY(scope, id));
                CREATE TABLE IF NOT EXISTS relevance_vectors(
                    scope TEXT NOT NULL, candidate_id TEXT NOT NULL, point TEXT NOT NULL,
                    signature TEXT NOT NULL, metric TEXT NOT NULL, vector BLOB NOT NULL,
                    PRIMARY KEY(scope, candidate_id, point),
                    FOREIGN KEY(scope, candidate_id) REFERENCES relevance_candidates(scope, id) ON DELETE CASCADE);
                CREATE INDEX IF NOT EXISTS relevance_point ON relevance_vectors(scope, point, signature);
                """;
            command.ExecuteNonQuery();
        }
        catch { connection.Dispose(); throw; }
    }

    /// <inheritdoc />
    public async ValueTask<T> TransactAsync<T>(Func<IRelevanceTransaction, T> action, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            // SQLite I/O is synchronous. No fake Task.Run or unbounded background queue.
            using var transaction = connection.BeginTransaction(deferred: false);
            var result = action(new Transaction(connection, transaction, native, recordTypes, vectorOptions, ReserveScope));
            cancellationToken.ThrowIfCancellationRequested();
            transaction.Commit();
            return result;
        }
        finally { gate.Release(); }
    }
    /// <summary>Waits for the active transaction and closes the database connection.</summary>
    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try
        {
            connection.Dispose();
            foreach (var lease in leases.Values) lease.Dispose();
            leases.Clear();
        }
        finally { gate.Release(); }
    }

    private void ReserveScope(string scope)
    {
        if (leaseRoot is null || leases.ContainsKey(scope)) return;
        var id = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(scope)));
        try { leases[scope] = new FileStream(leaseRoot + "." + id + ".writer.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException error) { throw new InvalidOperationException("Native vector space is already in use, or its ownership lock cannot be opened. Reuse its existing hub.", error); }
    }

    private sealed class Transaction(SqliteConnection connection, SqliteTransaction transaction, bool native, System.Text.Json.Serialization.JsonSerializerContext? recordTypes, SqliteVectorOptions? vectorOptions, Action<string> reserveScope) : IRelevanceTransaction
    {
        private readonly SqliteHnsw? hnsw = vectorOptions is null ? null : new(connection, transaction, vectorOptions);
        private SqliteCommand Command(string sql, params (string, object)[] args)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            foreach (var (name, value) in args) command.Parameters.AddWithValue(name, value);
            return command;
        }
        public void BindSchema(string scope, string fingerprint)
        {
            reserveScope(scope);
            using var read = Command("SELECT fingerprint FROM relevance_schema WHERE scope=@scope", ("@scope", scope));
            if (read.ExecuteScalar() is string current)
            {
                if (current != fingerprint) throw new InvalidOperationException("Configuration/converter version changed; use a new engine ID or migrate state.");
                return;
            }
            using var insert = Command("INSERT INTO relevance_schema VALUES(@scope,@fingerprint)", ("@scope", scope), ("@fingerprint", fingerprint));
            insert.ExecuteNonQuery();
        }
        public bool Accept(string scope, string id, string hash, DateTimeOffset expiresAt)
        {
            var current = Get<string>(scope, "dedup", id);
            if (current is not null && current != hash) throw new ArgumentException("Event ID reused with different content.");
            if (current is not null) return false;
            Put(scope, "dedup", id, hash, expiresAt);
            return true;
        }
        public T? Get<T>(string scope, string kind, string id)
        {
            using var command = kind == "candidate"
                ? Command("SELECT record FROM relevance_candidates WHERE scope=@scope AND id=@id", ("@scope", scope), ("@id", id))
                : Command("SELECT payload FROM relevance_records WHERE scope=@scope AND kind=@kind AND id=@id", ("@scope", scope), ("@kind", kind), ("@id", id));
            return command.ExecuteScalar() is string json ? RelevanceJson.Deserialize<T>(json, recordTypes) : default;
        }
        public void Put<T>(string scope, string kind, string id, T value, DateTimeOffset expiresAt)
        {
            using var command = Command("""
                INSERT INTO relevance_records VALUES(@scope,@kind,@id,@payload,@expires)
                ON CONFLICT(scope,kind,id) DO UPDATE SET payload=excluded.payload,expires=excluded.expires
                """, ("@scope", scope), ("@kind", kind), ("@id", id), ("@payload", RelevanceJson.Serialize(value, recordTypes)), ("@expires", expiresAt.UtcTicks));
            command.ExecuteNonQuery();
        }
        public int Count(string scope, string kind)
        {
            using var command = kind == "candidate"
                ? Command("SELECT COUNT(*) FROM relevance_candidates WHERE scope=@scope", ("@scope", scope))
                : Command("SELECT COUNT(*) FROM relevance_records WHERE scope=@scope AND kind=@kind", ("@scope", scope), ("@kind", kind));
            return checked((int)(long)command.ExecuteScalar()!);
        }
        public void Upsert(string scope, Candidate candidate)
        {
            reserveScope(scope);
            if (hnsw is not null && Get<Candidate>(scope, "candidate", candidate.Id) is { } old) hnsw.Delete(scope, old);
            using var command = Command("""
                INSERT INTO relevance_candidates VALUES(@scope,@id,@payload,@record)
                ON CONFLICT(scope,id) DO UPDATE SET payload=excluded.payload,record=excluded.record
                """, ("@scope", scope), ("@id", candidate.Id), ("@payload", candidate.Data.GetRawText()), ("@record", RelevanceJson.Serialize(candidate)));
            command.ExecuteNonQuery();
            using var delete = Command("DELETE FROM relevance_vectors WHERE scope=@scope AND candidate_id=@id", ("@scope", scope), ("@id", candidate.Id));
            delete.ExecuteNonQuery();
            foreach (var (name, point) in candidate.Points)
            {
                using var insert = Command("INSERT INTO relevance_vectors VALUES(@scope,@id,@point,@signature,@metric,@vector)",
                    ("@scope", scope), ("@id", candidate.Id), ("@point", name), ("@signature", point.Signature),
                    ("@metric", point.Metric), ("@vector", VectorMath.Encode(point.Values)));
                insert.ExecuteNonQuery();
            }
            hnsw?.Upsert(scope, candidate);
        }
        public IReadOnlyList<Neighbor> Nearest(string scope, VectorQuery query, Filter? filter, int limit)
        {
            reserveScope(scope);
            if (hnsw is not null && (filter is null || filter is Conjunction { Filters.Count: 0 })) return hnsw.Nearest(scope, query, limit);
            var compiled = SqliteFilterCompiler.Compile(filter);
            var distance = native ? query.Metric switch
            {
                "cosine" => "vec_distance_cosine(v.vector,@query)",
                "l2" => "vec_distance_L2(v.vector,@query)",
                _ => throw new ArgumentException("Unsupported native vector metric.")
            } : "relevance_distance(v.vector,@query,@metric)";
            using var command = Command($"""
                SELECT c.record, {distance} AS distance
                FROM relevance_candidates c JOIN relevance_vectors v
                  ON c.scope=v.scope AND c.id=v.candidate_id
                WHERE c.scope=@scope AND v.point=@point AND v.signature=@signature AND {compiled.Sql}
                ORDER BY distance,c.id COLLATE RELEVANCE_ORDINAL LIMIT @limit
                """, ("@scope", scope), ("@point", query.Point), ("@signature", query.Signature),
                ("@query", VectorMath.Encode(query.Values)), ("@limit", limit));
            if (!native) command.Parameters.AddWithValue("@metric", query.Metric);
            Bind(command, compiled);
            using var reader = command.ExecuteReader();
            var results = new List<Neighbor>();
            while (reader.Read()) results.Add(new Neighbor(RelevanceJson.Deserialize<Candidate>(reader.GetString(0))!, reader.GetDouble(1)));
            return results;
        }
        public IReadOnlyList<Candidate> Eligible(string scope, Filter? filter, int limit)
        {
            var compiled = SqliteFilterCompiler.Compile(filter);
            using var command = Command($"SELECT c.record FROM relevance_candidates c WHERE c.scope=@scope AND {compiled.Sql} ORDER BY c.id COLLATE RELEVANCE_ORDINAL LIMIT @limit",
                ("@scope", scope), ("@limit", limit));
            Bind(command, compiled);
            using var reader = command.ExecuteReader();
            var results = new List<Candidate>();
            while (reader.Read()) results.Add(RelevanceJson.Deserialize<Candidate>(reader.GetString(0))!);
            return results;
        }
        private static void Bind(SqliteCommand command, CompiledFilter filter)
        {
            foreach (var (name, value) in filter.Parameters) command.Parameters.AddWithValue(name, value);
        }
        public void Prune(DateTimeOffset now)
        {
            using var command = Command("DELETE FROM relevance_records WHERE expires<=@now", ("@now", now.UtcTicks));
            command.ExecuteNonQuery();
        }
    }
}
