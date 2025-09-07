
namespace BankingSystem.Utils
{
    public static class Input
    {
        public static string Prompt(string label, bool required = true)
        {
            while (true)
            {
                Console.Write(label);
                var s = Console.ReadLine() ?? string.Empty;
                if (!required || !string.IsNullOrWhiteSpace(s)) return s.Trim();
                Console.WriteLine("Value cannot be empty.");
            }
        }

        public static decimal PromptMoney(string label)
        {
            while (true)
            {
                Console.Write(label);
                var s = (Console.ReadLine() ?? string.Empty).Trim();
                if (decimal.TryParse(s, out var val) && val >= 0)
                    return decimal.Round(val, 2);
                Console.WriteLine("Enter a valid non-negative number (e.g., 100.50).");
            }
        }

        public static int PromptInt(string label, int min = int.MinValue, int max = int.MaxValue)
        {
            while (true)
            {
                Console.Write(label);
                var s = (Console.ReadLine() ?? string.Empty).Trim();
                if (int.TryParse(s, out var val) && val >= min && val <= max)
                    return val;
                Console.WriteLine($"Enter an integer between {min} and {max}.");
            }
        }
    }
}
