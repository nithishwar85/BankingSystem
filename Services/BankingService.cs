
using Microsoft.Data.Sqlite;
using BankingSystem.Data;
using BankingSystem.Models;

namespace BankingSystem.Services
{
    public class BankingService
    {
        public Account CreateAccount(int userId)
        {
            using var conn = new SqliteConnection(Database.ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            var accountNumber = GenerateAccountNumber(conn, tx);

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO Accounts (UserId, AccountNumber, Balance, CreatedAt)
                                    VALUES ($u, $n, 0, $c);
                                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("$u", userId);
                cmd.Parameters.AddWithValue("$n", accountNumber);
                cmd.Parameters.AddWithValue("$c", DateTime.UtcNow.ToString("o"));
                var id = (long)cmd.ExecuteScalar()!;

                tx.Commit();
                return new Account { Id = (int)id, UserId = userId, AccountNumber = accountNumber, Balance = 0m, CreatedAt = DateTime.UtcNow };
            }
        }

        private string GenerateAccountNumber(SqliteConnection conn, SqliteTransaction tx)
        {
            // Simple unique number: 10 digits starting with 10
            var rnd = new Random();
            while (true)
            {
                var candidate = "10" + rnd.Next(100000000, 999999999).ToString();
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT COUNT(1) FROM Accounts WHERE AccountNumber = $n";
                cmd.Parameters.AddWithValue("$n", candidate);
                var exists = Convert.ToInt32((long)cmd.ExecuteScalar()!) > 0;
                if (!exists) return candidate;
            }
        }

        public IEnumerable<Account> GetAccountsForUser(int userId)
        {
            using var conn = new SqliteConnection(Database.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, UserId, AccountNumber, Balance, CreatedAt FROM Accounts WHERE UserId = $u ORDER BY Id";
            cmd.Parameters.AddWithValue("$u", userId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                yield return new Account
                {
                    Id = rdr.GetInt32(0),
                    UserId = rdr.GetInt32(1),
                    AccountNumber = rdr.GetString(2),
                    Balance = (decimal)rdr.GetDouble(3),
                    CreatedAt = DateTime.Parse(rdr.GetString(4))
                };
            }
        }

        public IEnumerable<Account> GetAllAccounts()
        {
            using var conn = new SqliteConnection(Database.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, UserId, AccountNumber, Balance, CreatedAt FROM Accounts ORDER BY Id";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                yield return new Account
                {
                    Id = rdr.GetInt32(0),
                    UserId = rdr.GetInt32(1),
                    AccountNumber = rdr.GetString(2),
                    Balance = (decimal)rdr.GetDouble(3),
                    CreatedAt = DateTime.Parse(rdr.GetString(4))
                };
            }
        }

        public bool Deposit(string accountNumber, decimal amount, string? note = null)
        {
            if (amount <= 0) return false;
            using var conn = new SqliteConnection(Database.ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            var acc = GetAccountByNumber(conn, tx, accountNumber, forUpdate: true);
            if (acc is null) return false;

            acc.Balance += amount;
            UpdateBalance(conn, tx, acc.Id, acc.Balance);
            InsertTransaction(conn, tx, acc.Id, TransactionType.Deposit, amount, note, null, acc.Balance);

            tx.Commit();
            return true;
        }

        public bool Withdraw(string accountNumber, decimal amount, string? note = null)
        {
            if (amount <= 0) return false;
            using var conn = new SqliteConnection(Database.ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            var acc = GetAccountByNumber(conn, tx, accountNumber, forUpdate: true);
            if (acc is null) return false;
            if (acc.Balance < amount) return false;

            acc.Balance -= amount;
            UpdateBalance(conn, tx, acc.Id, acc.Balance);
            InsertTransaction(conn, tx, acc.Id, TransactionType.Withdrawal, amount, note, null, acc.Balance);

            tx.Commit();
            return true;
        }

        public bool Transfer(string fromAccountNumber, string toAccountNumber, decimal amount, string? note = null)
        {
            if (amount <= 0 || fromAccountNumber == toAccountNumber) return false;
            using var conn = new SqliteConnection(Database.ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            var from = GetAccountByNumber(conn, tx, fromAccountNumber, forUpdate: true);
            var to = GetAccountByNumber(conn, tx, toAccountNumber, forUpdate: true);
            if (from is null || to is null) return false;
            if (from.Balance < amount) return false;

            from.Balance -= amount;
            to.Balance += amount;

            UpdateBalance(conn, tx, from.Id, from.Balance);
            UpdateBalance(conn, tx, to.Id, to.Balance);

            InsertTransaction(conn, tx, from.Id, TransactionType.Transfer, amount, note, to.AccountNumber, from.Balance);
            InsertTransaction(conn, tx, to.Id, TransactionType.Deposit, amount, note, from.AccountNumber, to.Balance);

            tx.Commit();
            return true;
        }

        public IEnumerable<TransactionRecord> GetTransactions(string accountNumber, int limit = 50)
        {
            using var conn = new SqliteConnection(Database.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT t.Id, t.AccountId, t.Type, t.Amount, t.Timestamp, t.Note, t.RelatedAccountNumber, t.BalanceAfter
                FROM Transactions t
                JOIN Accounts a ON a.Id = t.AccountId
                WHERE a.AccountNumber = $n
                ORDER BY t.Id DESC
                LIMIT $lim";
            cmd.Parameters.AddWithValue("$n", accountNumber);
            cmd.Parameters.AddWithValue("$lim", limit);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                yield return new TransactionRecord
                {
                    Id = rdr.GetInt32(0),
                    AccountId = rdr.GetInt32(1),
                    Type = (TransactionType)rdr.GetInt32(2),
                    Amount = (decimal)rdr.GetDouble(3),
                    Timestamp = DateTime.Parse(rdr.GetString(4)),
                    Note = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    RelatedAccountNumber = rdr.IsDBNull(6) ? null : rdr.GetString(6),
                    BalanceAfter = (decimal)rdr.GetDouble(7)
                };
            }
        }

        private Account? GetAccountByNumber(SqliteConnection conn, SqliteTransaction tx, string accountNumber, bool forUpdate = false)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            // SQLite doesn't support SELECT ... FOR UPDATE, but transaction with immediate lock ensures consistency
            cmd.CommandText = "SELECT Id, UserId, AccountNumber, Balance, CreatedAt FROM Accounts WHERE AccountNumber = $n LIMIT 1";
            cmd.Parameters.AddWithValue("$n", accountNumber);
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                return new Account
                {
                    Id = rdr.GetInt32(0),
                    UserId = rdr.GetInt32(1),
                    AccountNumber = rdr.GetString(2),
                    Balance = (decimal)rdr.GetDouble(3),
                    CreatedAt = DateTime.Parse(rdr.GetString(4))
                };
            }
            return null;
        }

        private void UpdateBalance(SqliteConnection conn, SqliteTransaction tx, int accountId, decimal newBalance)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Accounts SET Balance = $b WHERE Id = $id";
            cmd.Parameters.AddWithValue("$b", newBalance);
            cmd.Parameters.AddWithValue("$id", accountId);
            cmd.ExecuteNonQuery();
        }

        private void InsertTransaction(SqliteConnection conn, SqliteTransaction tx, int accountId, TransactionType type, decimal amount, string? note, string? relatedAccount, decimal balanceAfter)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO Transactions (AccountId, Type, Amount, Timestamp, Note, RelatedAccountNumber, BalanceAfter)
                                VALUES ($a, $t, $amt, $ts, $n, $rel, $bal)";
            cmd.Parameters.AddWithValue("$a", accountId);
            cmd.Parameters.AddWithValue("$t", (int)type);
            cmd.Parameters.AddWithValue("$amt", amount);
            cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("$n", (object?)note ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$rel", (object?)relatedAccount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$bal", balanceAfter);
            cmd.ExecuteNonQuery();
        }
    }
}
