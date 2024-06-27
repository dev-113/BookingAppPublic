namespace BookingApp.Helper
{
    public struct Constants
    {
        public struct Status
        {
            public const string Confirmed = "Confirmed";
            public const string Pending = "Pending";
        }

        public struct Role
        {
            public const string Admin = "Admin";
            public const string User = "User";
        }

        public struct Email
        {
            public const string DevMail = "dev-113@hotmail.com";
        }

        public struct AppSettings
        {
            public const string MailSettings = "MailSettings";
        }

        public struct BookingType
        {
            public const string Standard = "Standard";
            public const string Serie = "Serie";
        }
    }
}
