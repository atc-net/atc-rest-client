namespace Atc.Rest.Client.Streaming;

internal static class StreamRecordLimit
{
    public static int Validate(int maxStreamRecordLength)
        => maxStreamRecordLength > 0
            ? maxStreamRecordLength
            : throw new ArgumentOutOfRangeException(
                nameof(maxStreamRecordLength),
                maxStreamRecordLength,
                "The longest record of a stream (MaxStreamRecordLength of AtcRestClientOptions, or MaxRecordLength of StreamReadOptions) must be greater than zero.");

    public static InvalidDataException CreateException(
        string description,
        int maxRecordLength)
        => new(
            $"{description} is longer than {maxRecordLength} bytes. " +
            "The limit is MaxStreamRecordLength of AtcRestClientOptions, or MaxRecordLength of StreamReadOptions for one call.");
}