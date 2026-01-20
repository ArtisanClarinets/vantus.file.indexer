
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Vantus.Engine.Services;

public class DatabaseService
{
    private readonly ILogger<DatabaseService> _logger;
    private const string DbFileName = "index.db";
    private readonly string _connectionString;

    public DatabaseService(ILogger<DatabaseService> logger, string? dbPath = null)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        _logger = logger;

        if (string.IsNullOrEmpty(dbPath))
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vantus", "Index");
            Directory.CreateDirectory(folder);
            dbPath = Path.Combine(folder, DbFileName);
        }

        _connectionString = $"Data Source={dbPath}";
    }

    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Create FTS5 table for full-text search
        var sql = @"
            CREATE TABLE IF NOT EXISTS files (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                path TEXT NOT NULL UNIQUE,
                name TEXT NOT NULL,
                extension TEXT,
                size INTEGER,
                last_modified INTEGER,
                content TEXT
            );

            CREATE VIRTUAL TABLE IF NOT EXISTS files_fts USING fts5(
                name,
                content,
                content='files',
                content_rowid='id'
            );

            CREATE TRIGGER IF NOT EXISTS files_ai AFTER INSERT ON files BEGIN
              INSERT INTO files_fts(rowid, name, content) VALUES (new.id, new.name, new.content);
            END;

            CREATE TRIGGER IF NOT EXISTS files_ad AFTER DELETE ON files BEGIN
              INSERT INTO files_fts(files_fts, rowid, name, content) VALUES('delete', old.id, old.name, old.content);
            END;

            CREATE TRIGGER IF NOT EXISTS files_au AFTER UPDATE ON files BEGIN
              INSERT INTO files_fts(files_fts, rowid, name, content) VALUES('delete', old.id, old.name, old.content);
              INSERT INTO files_fts(rowid, name, content) VALUES (new.id, new.name, new.content);
            END;

            CREATE TABLE IF NOT EXISTS tags (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                type TEXT NOT NULL DEFAULT 'user' -- user, ai, rule
            );

            CREATE TABLE IF NOT EXISTS file_tags (
                file_id INTEGER NOT NULL,
                tag_id INTEGER NOT NULL,
                confidence REAL DEFAULT 1.0,
                PRIMARY KEY (file_id, tag_id),
                FOREIGN KEY(file_id) REFERENCES files(id) ON DELETE CASCADE,
                FOREIGN KEY(tag_id) REFERENCES tags(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS rules (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                condition_type TEXT NOT NULL, -- extension, size, name
                condition_value TEXT NOT NULL,
                action_type TEXT NOT NULL, -- tag
                action_value TEXT NOT NULL,
                is_active BOOLEAN DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS partners (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                domains TEXT, -- comma separated
                keywords TEXT -- comma separated
            );

            CREATE TABLE IF NOT EXISTS file_partners (
                file_id INTEGER NOT NULL,
                partner_id INTEGER NOT NULL,
                confidence REAL DEFAULT 1.0,
                PRIMARY KEY (file_id, partner_id),
                FOREIGN KEY(file_id) REFERENCES files(id) ON DELETE CASCADE,
                FOREIGN KEY(partner_id) REFERENCES partners(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS action_log (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                file_path TEXT NOT NULL,
                action_type TEXT NOT NULL,
                description TEXT,
                timestamp INTEGER NOT NULL,
                status TEXT
            );
        ";

        await connection.ExecuteAsync(sql);
        _logger.LogInformation("Database initialized at {ConnectionString}", _connectionString);
    }

    public SqliteConnection GetConnection() => new SqliteConnection(_connectionString);
}
