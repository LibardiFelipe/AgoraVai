using System.Diagnostics.CodeAnalysis;

namespace AgoraVai.Utils
{
    public sealed class Result<TContent>
    {
        private Result(bool isSuccess, TContent? content)
        {
            IsSuccess = isSuccess;
            Content = content;
        }

        [MemberNotNullWhen(true, nameof(Content))]
        public bool IsSuccess { get; init; }
        public TContent? Content { get; init; }

        public static Result<TContent> Success(TContent content) =>
            new(true, content);

        public static Result<TContent> Failure() =>
            new(false, default);
    }
}
