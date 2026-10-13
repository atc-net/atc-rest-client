namespace Atc.Rest.Client.Streaming;

/// <summary>
/// Reads the lines of a UTF-8 stream as bytes. A line ends at a line feed, and a carriage return before it belongs
/// to the line ending; the last line needs none. A UTF-8 byte order mark at the start of the stream is skipped.
/// A line longer than the limit is an <see cref="InvalidDataException"/> before it is buffered.
/// </summary>
/// <remarks>
/// With <c>carriageReturnEndsLine</c> a carriage return on its own ends a line as well, as the server-sent events
/// format has it. Such a line is returned at once, without waiting for the byte after it.
/// </remarks>
internal sealed class Utf8LineReader
{
    private const int ReadBufferSize = 4096;
    private const byte LineFeed = (byte)'\n';
    private const byte CarriageReturn = (byte)'\r';

    private readonly Stream stream;
    private readonly int maxLength;
    private readonly bool carriageReturnEndsLine;
    private readonly RecordBuffer line;
    private readonly byte[] readBuffer = new byte[ReadBufferSize];
    private int position;
    private int available;
    private int lineOffset;
    private bool isFirstLine = true;
    private bool skipLineFeed;

    public Utf8LineReader(
        Stream stream,
        int maxLength,
        string description,
        bool carriageReturnEndsLine = false)
    {
        this.stream = stream;
        this.maxLength = maxLength;
        this.carriageReturnEndsLine = carriageReturnEndsLine;
        line = new RecordBuffer(maxLength, description, slack: 1);
    }

    /// <summary>
    /// Gets the line that was read, without its line ending. It is valid until the next line is read.
    /// </summary>
    public ArraySegment<byte> Line => line.GetSegment(lineOffset);

    public async ValueTask<bool> ReadLineAsync(
        CancellationToken cancellationToken)
    {
        line.Clear();
        lineOffset = 0;

        while (true)
        {
            if (position == available)
            {
                cancellationToken.ThrowIfCancellationRequested();
                available = await stream.ReadChunkAsync(readBuffer, cancellationToken).ConfigureAwait(false);
                position = 0;
                if (available == 0)
                {
                    if (line.Length == 0)
                    {
                        return false;
                    }

                    EndLine();
                    return true;
                }
            }

            if (skipLineFeed)
            {
                // The line feed of a CRLF whose carriage return already ended the line
                skipLineFeed = false;
                if (readBuffer[position] == LineFeed)
                {
                    position++;
                    continue;
                }
            }

            var end = IndexOfLineEnd();
            line.Append(new ArraySegment<byte>(readBuffer, position, end - position));
            position = end;

            if (end < available)
            {
                skipLineFeed = carriageReturnEndsLine && readBuffer[end] == CarriageReturn;
                position++;
                EndLine();
                return true;
            }
        }
    }

    private int IndexOfLineEnd()
    {
        var unread = readBuffer.AsSpan(position, available - position);
        var index = carriageReturnEndsLine
            ? unread.IndexOfAny(LineFeed, CarriageReturn)
            : unread.IndexOf(LineFeed);

        return index < 0
            ? available
            : position + index;
    }

    private void EndLine()
    {
        if (line.Length > 0 && line[line.Length - 1] == CarriageReturn)
        {
            line.RemoveLast();
        }

        if (line.Length > maxLength)
        {
            throw line.CreateTooLongException();
        }

        if (!isFirstLine)
        {
            return;
        }

        isFirstLine = false;
        if (line.Length >= 3 && line[0] == 0xEF && line[1] == 0xBB && line[2] == 0xBF)
        {
            lineOffset = 3;
        }
    }
}