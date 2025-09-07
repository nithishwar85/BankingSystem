
using BankingSystem.Data;
using BankingSystem.Models;
using BankingSystem.Services;
using BankingSystem.Utils;

Database.Initialize();
var auth = new AuthService();
var bank = new BankingService();

User? currentUser = null;

while (true)
{
    if (currentUser is null)
    {
        Console.WriteLine("\n=== BANKING SYSTEM ===");
        Console.WriteLine("1) Login");
        Console.WriteLine("2) Exit");
        Console.Write("Select: ");
        var c = Console.ReadLine();
        if (c == "1")
        {
            var u = Input.Prompt("Username: ");
            var p = Input.Prompt("Password: ");
            var logged = auth.Login(u, p);
            if (logged is null)
            {
                Console.WriteLine("Invalid credentials.");
            }
            else
            {
                currentUser = logged;
                Console.WriteLine($"Welcome, {currentUser.Username}! Role: {currentUser.Role}");
            }
        }
        else if (c == "2") break;
        else Console.WriteLine("Invalid choice.");
        continue;
    }

    if (currentUser.Role == Role.Admin)
    {
        Console.WriteLine("\n=== ADMIN MENU ===");
        Console.WriteLine("1) Create user");
        Console.WriteLine("2) List users");
        Console.WriteLine("3) List all accounts");
        Console.WriteLine("4) Logout");
        Console.Write("Select: ");
        var a = Console.ReadLine();
        switch (a)
        {
            case "1":
                var nu = Input.Prompt("New username: ");
                var np = Input.Prompt("New password: ");
                Console.Write("Role (0=Admin, 1=Customer): ");
                var rs = Console.ReadLine();
                if (!int.TryParse(rs, out var r) || (r != 0 && r != 1))
                {
                    Console.WriteLine("Invalid role.");
                    break;
                }
                try
                {
                    var user = auth.CreateUser(nu, np, (Role)r);
                    Console.WriteLine($"Created user #{user.Id} ({user.Username}) with role {user.Role}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create user: {ex.Message}");
                }
                break;
            case "2":
                Console.WriteLine("Users:");
                foreach (var u in auth.GetAllUsers())
                    Console.WriteLine($"- {u.Id}: {u.Username} ({u.Role}) Created: {u.CreatedAt:u}");
                break;
            case "3":
                Console.WriteLine("Accounts:");
                foreach (var acc in bank.GetAllAccounts())
                    Console.WriteLine($"- {acc.AccountNumber} | UserId: {acc.UserId} | Balance: {acc.Balance:C} | Created: {acc.CreatedAt:u}");
                break;
            case "4":
                currentUser = null;
                break;
            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
    }
    else // Customer
    {
        Console.WriteLine("\n=== CUSTOMER MENU ===");
        Console.WriteLine("1) Create bank account");
        Console.WriteLine("2) View my accounts");
        Console.WriteLine("3) Deposit");
        Console.WriteLine("4) Withdraw");
        Console.WriteLine("5) Transfer");
        Console.WriteLine("6) View transactions");
        Console.WriteLine("7) Change password");
        Console.WriteLine("8) Logout");
        Console.Write("Select: ");
        var c = Console.ReadLine();
        switch (c)
        {
            case "1":
                var acc = bank.CreateAccount(currentUser.Id);
                Console.WriteLine($"Created account {acc.AccountNumber} with balance {acc.Balance:C}.");
                break;
            case "2":
                var my = bank.GetAccountsForUser(currentUser.Id).ToList();
                if (!my.Any()) { Console.WriteLine("No accounts yet."); break; }
                foreach (var a in my)
                    Console.WriteLine($"- {a.AccountNumber} | Balance: {a.Balance:C} | Created: {a.CreatedAt:u}");
                break;
            case "3":
                var an = Input.Prompt("Account number: ");
                var amt = Input.PromptMoney("Amount to deposit: ");
                Console.WriteLine(bank.Deposit(an, amt, "Cash deposit") ? "Deposit successful." : "Deposit failed.");
                break;
            case "4":
                var an2 = Input.Prompt("Account number: ");
                var wamt = Input.PromptMoney("Amount to withdraw: ");
                Console.WriteLine(bank.Withdraw(an2, wamt, "Cash withdrawal") ? "Withdrawal successful." : "Withdrawal failed.");
                break;
            case "5":
                var from = Input.Prompt("From account: ");
                var to = Input.Prompt("To account: ");
                var tamt = Input.PromptMoney("Amount to transfer: ");
                Console.WriteLine(bank.Transfer(from, to, tamt, "Transfer") ? "Transfer successful." : "Transfer failed.");
                break;
            case "6":
                var tacc = Input.Prompt("Account number: ");
                var txs = bank.GetTransactions(tacc, 100).ToList();
                if (!txs.Any()) { Console.WriteLine("No transactions."); break; }
                Console.WriteLine($"Last {txs.Count} transactions for {tacc}:");
                foreach (var t in txs)
                    Console.WriteLine($"{t.Timestamp:u} | {t.Type} | {t.Amount:C} | BalAfter: {t.BalanceAfter:C} | Note: {t.Note} | Related: {t.RelatedAccountNumber}");
                break;
            case "7":
                var np = Input.Prompt("New password: ");
                Console.WriteLine(auth.ChangePassword(currentUser.Id, np) ? "Password changed." : "Failed to change password.");
                break;
            case "8":
                currentUser = null;
                break;
            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
    }
}
