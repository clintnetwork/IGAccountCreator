namespace IGAccountCreator.Models
{
    public class AutomatorStatus
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public bool HasRegistredAccount { get; set; }
        public bool HasUpdatedProfilePicture { get; set; }
        public bool HasUpdatedBio { get; set; }
        public bool HasRenewedIp { get; set; }
        public string Details { get; set; }
        public string UsedIp { get; set; }
        public double Countdown { get; set; }
    }
}