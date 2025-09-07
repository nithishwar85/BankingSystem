
using Microsoft.Data.Sqlite;
using System.IO;

namespace BankingSystem.Data
{
    public static class Database
    {
        private static readonly string _dbPath = Path.Combine(AppContext.BaseDirectory, "banking.db");
        public static string ConnectionString => $"Data Source={_dbPath};Cache=Shared";

        public static void Initialize()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                Role INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Accounts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                AccountNumber TEXT NOT NULL UNIQUE,
                Balance REAL NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AccountId INTEGER NOT NULL,
                Type INTEGER NOT NULL,
                Amount REAL NOT NULL,
                Timestamp TEXT NOT NULL,
                Note TEXT,
                RelatedAccountNumber TEXT,
                BalanceAfter REAL NOT NULL,
                FOREIGN KEY(AccountId) REFERENCES Accounts(Id) ON DELETE CASCADE
            );
            ";
            cmd.ExecuteNonQuery();
        }
    }
}
