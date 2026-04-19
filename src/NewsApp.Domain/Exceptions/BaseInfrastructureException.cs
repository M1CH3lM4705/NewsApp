using System;

namespace NewsApp.Domain.Exceptions;

/// <summary>
/// Exceção base para erros ocorridos na camada de infraestrutura.
/// </summary>
public abstract class BaseInfrastructureException : Exception
{
    public string ServiceName { get; }

    protected BaseInfrastructureException(string message, string serviceName, Exception? innerException = null)
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}

/// <summary>
/// Exceção lançada quando ocorre um erro em um serviço externo (Ex: NewsAPI, Gemini).
/// </summary>
public class ExternalServiceException : BaseInfrastructureException
{
    public int? StatusCode { get; }
    public string? RawResponse { get; }

    public ExternalServiceException(string message, string serviceName, int? statusCode = null, string? rawResponse = null, Exception? innerException = null)
        : base(message, serviceName, innerException)
    {
        StatusCode = statusCode;
        RawResponse = rawResponse;
    }
}

/// <summary>
/// Exceção específica para falhas no processo de tradução e resumo por IA.
/// </summary>
public class TranslationException : BaseInfrastructureException
{
    public TranslationException(string message, string serviceName, Exception? innerException = null)
        : base(message, serviceName, innerException)
    {
    }
}
