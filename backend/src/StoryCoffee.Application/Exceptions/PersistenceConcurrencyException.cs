namespace StoryCoffee.Application.Exceptions;

public sealed class PersistenceConcurrencyException(string message, Exception innerException) : Exception(message, innerException);
