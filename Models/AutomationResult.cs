namespace IGAccountCreator.Models
{
    public class AutomationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }

        public static AutomationResult Error(string message) =>
            new AutomationResult
            {
                IsSuccess = false,
                ErrorMessage = message
            };

        public static AutomationResult Ok() =>
            new AutomationResult
            {
                IsSuccess = true
            };
    }
}