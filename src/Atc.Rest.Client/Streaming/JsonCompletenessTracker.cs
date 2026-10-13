namespace Atc.Rest.Client.Streaming;

/// <summary>
/// Follows the bytes of one JSON text as they arrive and tells when it is balanced: objects and arrays closed, no
/// open string, and something besides whitespace. The structural characters are ASCII, so the bytes of a multi-byte
/// UTF-8 character never look like one.
/// </summary>
internal sealed class JsonCompletenessTracker
{
    private int depth;
    private bool inString;
    private bool escaped;

    public bool HasContent { get; private set; }

    public bool IsComplete => HasContent && depth == 0 && !inString;

    public void Reset()
    {
        depth = 0;
        inString = false;
        escaped = false;
        HasContent = false;
    }

    public void Add(ArraySegment<byte> bytes)
    {
        for (var i = bytes.Offset; i < bytes.Offset + bytes.Count; i++)
        {
            Add(bytes.Array![i]);
        }
    }

    public void Add(byte value)
    {
        if (inString)
        {
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
            case (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n':
                return;
            case (byte)'"':
                inString = true;
                break;
            case (byte)'{' or (byte)'[':
                depth++;
                break;
            case (byte)'}' or (byte)']':
                depth--;
                break;
        }

        HasContent = true;
    }
}