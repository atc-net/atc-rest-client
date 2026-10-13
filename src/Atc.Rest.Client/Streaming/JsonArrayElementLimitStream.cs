namespace Atc.Rest.Client.Streaming;

/// <summary>
/// Limits the length of one element of the JSON array a stream holds. An element is counted from its first byte to
/// the comma or the closing bracket after it; whitespace before an element is not counted.
/// </summary>
internal sealed class JsonArrayElementLimitStream : RecordLimitStream
{
    private int depth;
    private bool inString;
    private bool escaped;
    private long length;

    public JsonArrayElementLimitStream(
        Stream inner,
        int maxLength)
        : base(inner, maxLength, "An element of the JSON array stream")
    {
    }

    protected override long RecordLength => length;

    protected override int Count(ReadOnlySpan<byte> bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            Count(bytes[i]);
            if (length > MaxLength)
            {
                return i;
            }
        }

        return bytes.Length;
    }

    private void Count(byte value)
    {
        if (inString)
        {
            length++;
            if (escaped)
            {
                escaped = false;
            }
            else if (value == (byte)'\\')
            {
                escaped = true;
            }
            else if (value == (byte)'"')
            {
                inString = false;
            }

            return;
        }

        switch (value)
        {
            case (byte)'"':
                inString = true;
                length++;
                break;
            case (byte)'[' or (byte)'{':
                depth++;
                if (depth > 1)
                {
                    length++;
                }

                break;
            case (byte)']' or (byte)'}':
                depth--;
                length = depth >= 1 ? length + 1 : 0;
                break;
            case (byte)',' when depth == 1:
                length = 0;
                break;
            case (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' when length == 0:
                break;
            default:
                if (depth >= 1)
                {
                    length++;
                }

                break;
        }
    }
}