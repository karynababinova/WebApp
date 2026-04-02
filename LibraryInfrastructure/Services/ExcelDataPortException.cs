namespace LibraryInfrastructure.Services;

public class ExcelDataPortException : Exception
{
    public ExcelDataPortException(string message)
        : base(message)
    {
    }
}
