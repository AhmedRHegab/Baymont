namespace Baymont.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string message = "السجل غير موجود.")
        : base(message)
    {
    }
}
