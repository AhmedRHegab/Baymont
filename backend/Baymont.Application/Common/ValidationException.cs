namespace Baymont.Application.Common;

/// <summary>The data is wrong in a way the user can fix. The message is Arabic, short and safe to show as is.</summary>
public class ValidationException(string message) : Exception(message);
