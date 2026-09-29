namespace MicroShop.BuildingBlocks.Domain;

public sealed class Result<TValue> : Result
{
    private readonly TValue? value;

    internal Result(
        TValue? value,
        bool isSuccess,
        Error error)
        : base(
            isSuccess,
            error)
    {
        this.value = value;
    }

    public TValue Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("A failure result has no value.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
