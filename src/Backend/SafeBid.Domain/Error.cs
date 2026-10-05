namespace SafeBid.Domain;

public class Error
{
    public string Code { get; }
    public string Message { get; }

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static readonly Error None = new(string.Empty, string.Empty);
}

public static class DomainErrors
{
    public static class User
    {
        public static readonly Error DuplicateEmail = new("User.DuplicateEmail", "The email address is already in use.");
        public static readonly Error DuplicatePhone = new("User.DuplicatePhone", "The phone number is already in use.");
        public static readonly Error PasswordMismatch = new("User.PasswordMismatch", "The passwords do not match.");
    }
}
