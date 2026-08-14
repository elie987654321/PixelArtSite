namespace PixelArt.Core.Application.Exceptions;

// Base for expected use-case outcomes that abort an operation — a taken
// username, bad credentials, a missing record. The Interface layer maps these
// onto status codes; anything not deriving from this is a genuine 500.
public abstract class UseCaseException : Exception
{
    protected UseCaseException(string message) : base(message)
    {
    }
}
