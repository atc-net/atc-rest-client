namespace Atc.Rest.Client.Streaming;

/// <summary>
/// The bytes of the record being read. It refuses to grow past the limit, so a record that never ends is an
/// <see cref="InvalidDataException"/> and not a buffer that grows until the process is out of memory.
/// </summary>
internal sealed class RecordBuffer
{
    private readonly int maxLength;
    private readonly long capacityLimit;
    private readonly string description;
    private byte[] buffer = new byte[256];

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordBuffer"/> class.
    /// </summary>
    /// <param name="maxLength">The longest record, in bytes.</param>
    /// <param name="description">What a record is, for the exception message.</param>
    /// <param name="slack">Bytes taken past the limit before it is enforced (a carriage return that may turn out to end the line).</param>
    public RecordBuffer(
        int maxLength,
        string description,
        int slack = 0)
    {
        this.maxLength = maxLength;
        this.description = description;
        capacityLimit = (long)maxLength + slack;
    }

    public int Length { get; private set; }

    public byte this[int index] => buffer[index];

    public ArraySegment<byte> GetSegment(int offset = 0)
        => new(buffer, offset, Length - offset);

    public byte[] ToArray()
    {
        var bytes = new byte[Length];
        Array.Copy(buffer, bytes, Length);
        return bytes;
    }

    public void Clear()
        => Length = 0;

    public void RemoveLast()
        => Length--;

    public void Append(byte value)
    {
        EnsureCapacity(Length + 1L);
        buffer[Length++] = value;
    }

    public void Append(ArraySegment<byte> bytes)
    {
        if (bytes.Count == 0)
        {
            return;
        }

        EnsureCapacity((long)Length + bytes.Count);
        Array.Copy(bytes.Array!, bytes.Offset, buffer, Length, bytes.Count);
        Length += bytes.Count;
    }

    public InvalidDataException CreateTooLongException()
        => StreamRecordLimit.CreateException(description, maxLength);

    private void EnsureCapacity(long needed)
    {
        if (needed > capacityLimit)
        {
            throw CreateTooLongException();
        }

        if (needed <= buffer.Length)
        {
            return;
        }

        var capacity = Math.Max(needed, Math.Min(buffer.Length * 2L, capacityLimit));
        Array.Resize(ref buffer, (int)capacity);
    }
}