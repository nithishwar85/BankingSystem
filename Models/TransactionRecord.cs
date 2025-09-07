
namespace BankingSystem.Models
{
    public class TransactionRecord
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
        public string? RelatedAccountNumber { get; set; }
        public decimal BalanceAfter { get; set; }
    }
}
