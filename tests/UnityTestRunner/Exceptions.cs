namespace Alchemy.UnityTestRunner;

public class UnityTestException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);

public sealed class InvalidConfigurationException(string message)
    : UnityTestException(message);

public sealed class UnityUnavailableException(
    string message,
    Exception? innerException = null)
    : UnityTestException(message, innerException);

public sealed class UnityExecutionException(
    string message,
    Exception? innerException = null)
    : UnityTestException(message, innerException);

public sealed class ReportException(
    string message,
    Exception? innerException = null)
    : UnityTestException(message, innerException);

public sealed class ProcessExecutionException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);
