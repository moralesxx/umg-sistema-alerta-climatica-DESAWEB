namespace AlertaClimatica.Infrastructure.Exceptions;

// Igual que ArgumentException se mapea a 400 y UnauthorizedAccessException
// a 401 en los controllers, esta excepción se mapea a 409 Conflict —
// por ejemplo, correo ya registrado.
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}