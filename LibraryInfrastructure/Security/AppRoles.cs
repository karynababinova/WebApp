namespace LibraryInfrastructure.Security;

public static class AppRoles
{
    public const string Reader = "Reader";
    public const string Author = "Author";
    public const string Admin = "Admin";

    public static bool IsValid(string? role)
    {
        return role is Reader or Author or Admin;
    }

    public static string Normalize(string? role)
    {
        return role switch
        {
            Admin => Admin,
            Author => Author,
            Reader => Reader,
            _ => Reader
        };
    }

    public static string ToDisplayName(string? role)
    {
        return Normalize(role) switch
        {
            Admin => "Адміністратор",
            Author => "Автор",
            _ => "Читач"
        };
    }
}
