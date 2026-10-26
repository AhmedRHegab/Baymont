namespace Baymont.Application.Common;

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message = "تم تعديل البيانات بواسطة مستخدم آخر. يرجى إعادة تحميل السجل.")
        : base(message)
    {
    }
}
