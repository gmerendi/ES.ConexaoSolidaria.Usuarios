using Usuarios.Domain.Shared.Resources; // Garante o acesso ao seu ErrorMessages

namespace Usuarios.Domain.Shared.Primitives
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? Error { get; }
        public string? ErrorCode { get; } 

        private Result(bool isSuccess, T? value, string? error, string? errorCode)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            ErrorCode = errorCode; 
        }

        public static Result<T> Success(T value) => new(true, value, null, null);

        // Altera o Failure para receber o código do erro e resolver a mensagem pelo Resource
        public static Result<T> Failure(string errorCode)
        {
            // Busca a mensagem amigável no Errors.resx através do código fornecido
            string mensagemDoResource = ErrorMessages.GetString(errorCode);

            return new(false, default, mensagemDoResource, errorCode);
        }
    }
}