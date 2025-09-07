
using Microsoft.Data.Sqlite;
using BankingSystem.Data;
using BankingSystem.Models;
using BCrypt.Net;

namespace BankingSystem.Services
{
    public class AuthService
    {
        public AuthService()
        {
            EnsureDefaultAdmin();
        }

        public void EnsureDefaultAdmin()
        {
            // Create default admin if not exists
            if (GetUserByUsername("admin") is null)
            {
                CreateUser("admin", "Admin@123", Role.Admin);
            }
        }

        public User? GetUserByUsername(string username)
        {
            using var conn = new SqliteConnection(Database.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, PasswordHash, Role, CreatedAt FROM Users WHERE Username = $u LIMIT 1";
            cmd.Parameters.AddWithValue("$u", username);
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                return new User
                {
                    Id = rdr.GetInt32(0),
                    Username = rdr.GetString(1),
                    PasswordHash = rdr.GetString(2),
                    Role = (Role)rdr.GetInt32(3),
                    CreatedAt = DateTime.Parse(rdr.GetString(4))
                };
            }
            return null;
        }

        public User CreateUser(string username, string password, Role role)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            using var conn = new SqliteConnection(Database.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Users (Username, PasswordHash, Role, CreatedAt)
                                VALUES ($u, $p, $r, $c);
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$u", username);
            cmd.Parameters.AddWithValue("$p", hash);
            cmd.Parameters.AddWithValue("$r", (int)role);
            cmd.Parameters.AddWithValue("$c", DateTime.UtcNow.ToString("o"));
            var id = (long)cmd.ExecuteScalar()!;
            return new User { Id = (int)id, Username = username, PasswordHash = hash, Role = role, CreatedAt = DateTime.UtcNow };
        }

        public bool ChangePassword(int userId, string newPassword)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            using var conn = new SqliteConnection(Database.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET PasswordHash = $p WHERE Id = $id";
            cmd.Parameters.AddWithValue("$p", hash);
            cmd.Parameters.AddWithValue("$id", userId);
            return cmd.ExecuteNonQuery() == 1;
        }

        public User? Login(string username, string password)
        {
            var user = GetUserByUsername(username);
            if (user is null) return null;
            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
        }

        public IEnumerable<User> GetAllUsers()
        {
            using var conn = new SqliteConnection(Database.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, PasswordHash, Role, CreatedAt FROM Users ORDER BY Id";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                yield return new User
                {
                    Id = rdr.GetInt32(0),
                    Username = rdr.GetString(1),
                    PasswordHash = rdr.GetString(2),
                    Role = (Role)rdr.GetInt32(3),
                    CreatedAt = DateTime.Parse(rdr.GetString(4))
                };
            }
        }
    }
}
