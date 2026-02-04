namespace Atc.Rest.Client.Builder;

internal class MessageRequestBuilder : IMessageRequestBuilder
{
    private readonly string template;
    private readonly IContractSerializer serializer;
    private readonly Dictionary<string, string> pathMapper;
    private readonly Dictionary<string, string> headerMapper;
    private readonly Dictionary<string, string> queryMapper;
    private readonly Dictionary<string, string> formFields;
    private readonly List<(Stream Stream, string Name, string FileName, string? ContentType)> streamFiles;
    private string? content;
    private List<IFormFile>? contentFormFiles;
    private (Stream Stream, string ContentType)? binaryContent;

    public MessageRequestBuilder(
        string pathTemplate,
        IContractSerializer serializer)
    {
        this.template = pathTemplate ?? throw new ArgumentNullException(nameof(pathTemplate));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        pathMapper = new Dictionary<string, string>(StringComparer.Ordinal);
        headerMapper = new Dictionary<string, string>(StringComparer.Ordinal);
        queryMapper = new Dictionary<string, string>(StringComparer.Ordinal);
        formFields = new Dictionary<string, string>(StringComparer.Ordinal);
        streamFiles = [];
        WithHeaderParameter("accept", "application/json");
    }

    /// <inheritdoc />
    public HttpCompletionOption HttpCompletionOption { get; private set; } = HttpCompletionOption.ResponseContentRead;

    [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "OK - ByteArrayContent can't be disposed.")]
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    public HttpRequestMessage Build(HttpMethod method)
    {
        var message = new HttpRequestMessage();
        foreach (var parameter in headerMapper)
        {
            message.Headers.Add(parameter.Key, parameter.Value);
        }

        message.RequestUri = BuildRequestUri();
        message.Method = method;

        if (content is not null)
        {
            message.Content = new StringContent(content);
            message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        }
        else if (binaryContent.HasValue)
        {
            var streamContent = new StreamContent(binaryContent.Value.Stream);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(binaryContent.Value.ContentType);
            message.Content = streamContent;
        }
        else if (streamFiles.Count > 0 || formFields.Count > 0)
        {
            var formDataContent = new MultipartFormDataContent();

            foreach (var formField in formFields)
            {
                formDataContent.Add(new StringContent(formField.Value), formField.Key);
            }

            foreach (var file in streamFiles)
            {
                var streamContent = new StreamContent(file.Stream);
                if (file.ContentType is not null)
                {
                    streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
                }

                formDataContent.Add(streamContent, file.Name, file.FileName);
            }

            message.Content = formDataContent;
        }
        else if (contentFormFiles is not null)
        {
            var formDataContent = new MultipartFormDataContent();
            foreach (var formFile in contentFormFiles)
            {
                byte[] bytes;
                using (var binaryReader = new BinaryReader(formFile.OpenReadStream()))
                {
                    bytes = binaryReader.ReadBytes((int)formFile.OpenReadStream().Length);
                }

                var bytesContent = new ByteArrayContent(bytes);
                formDataContent.Add(bytesContent, "Request", formFile.FileName);
            }

            message.Headers.Remove("accept");
            message.Headers.Add("accept", "application/octet-stream");
            message.Content = formDataContent;
        }

        return message;
    }

    public IMessageRequestBuilder WithBody<TBody>(
        TBody body)
    {
        if (body is null)
        {
            throw new ArgumentNullException(nameof(body));
        }

        switch (body)
        {
            case IFormFile formFile:
                contentFormFiles = new List<IFormFile>
                {
                    formFile,
                };
                break;
            case List<IFormFile> formFiles:
                contentFormFiles = new List<IFormFile>();
                contentFormFiles.AddRange(formFiles);
                break;
            default:
                content = serializer.Serialize(body);
                break;
        }

        return this;
    }

    public IMessageRequestBuilder WithBinaryBody(
        Stream stream,
        string? contentType = null)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        binaryContent = (stream, contentType ?? "application/octet-stream");
        return this;
    }

    public IMessageRequestBuilder WithPathParameter(
        string name,
        object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace", nameof(name));
        }

        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            throw new ArgumentException($"'{nameof(value)}' cannot be null or whitespace", nameof(value));
        }

        pathMapper[name] = value.ToString();

        return this;
    }

    public IMessageRequestBuilder WithHeaderParameter(
        string name,
        object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace", nameof(name));
        }

        if (value is null)
        {
            return this;
        }

        headerMapper[name] = value.ToString();

        return this;
    }

    public IMessageRequestBuilder WithQueryParameter(
        string name,
        object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace", nameof(name));
        }

        if (value is null)
        {
            return this;
        }

        var valueType = value.GetType();
        if (valueType.IsArray || valueType.IsGenericType)
        {
            var objects = ((IEnumerable)value).Cast<object>().ToArray();
            var sb = new StringBuilder();
            for (var i = 0; i < objects.Length; i++)
            {
                sb.Append(i == 0
                    ? Uri.EscapeDataString(objects[i].ToString())
                    : $"&{name}={Uri.EscapeDataString(objects[i].ToString())}");
            }

            queryMapper["#" + name] = sb.ToString();
        }
        else if (valueType.IsEnum)
        {
            queryMapper[name] = valueType
                .GetTypeInfo()
                .DeclaredMembers
                .FirstOrDefault(x => x.Name == value.ToString())
                ?.GetCustomAttribute<EnumMemberAttribute>(inherit: false)
                ?.Value ?? value.ToString();
        }
        else if (value is DateTime dt)
        {
            queryMapper[name] = dt.ToString("o");
        }
        else if (value is DateTimeOffset dto)
        {
            queryMapper[name] = dto.ToString("o");
        }
        else
        {
            queryMapper[name] = value.ToString();
        }

        return this;
    }

    private Uri BuildRequestUri()
    {
        var urlBuilder = new StringBuilder();

        urlBuilder.Append(template);
        foreach (var parameter in pathMapper)
        {
            urlBuilder.Replace($"{{{parameter.Key}}}", Uri.EscapeDataString(parameter.Value));
        }

        if (queryMapper.Count == 0)
        {
            return new Uri(urlBuilder.ToString(), UriKind.RelativeOrAbsolute);
        }

        urlBuilder.Append('?');
        urlBuilder.Append(string.Join("&", queryMapper.Select(BuildQueryKeyEqualValue)));

        return new Uri(urlBuilder.ToString(), UriKind.RelativeOrAbsolute);
    }

    private static string BuildQueryKeyEqualValue(
        KeyValuePair<string, string> pair)
        => pair.Key.StartsWith("#", StringComparison.Ordinal)
            ? $"{pair.Key.Replace("#", string.Empty)}={pair.Value}"
            : $"{pair.Key}={Uri.EscapeDataString(pair.Value)}";

    public IMessageRequestBuilder WithFile(
        Stream stream,
        string name,
        string fileName,
        string? contentType = null)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException($"'{nameof(fileName)}' cannot be null or whitespace", nameof(fileName));
        }

        streamFiles.Add((stream, name, fileName, contentType));
        return this;
    }

    public IMessageRequestBuilder WithFiles(
        IEnumerable<(Stream Stream, string Name, string FileName, string? ContentType)> files)
    {
        if (files is null)
        {
            throw new ArgumentNullException(nameof(files));
        }

        foreach (var file in files)
        {
            WithFile(file.Stream, file.Name, file.FileName, file.ContentType);
        }

        return this;
    }

    public IMessageRequestBuilder WithFormField(
        string name,
        string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace", nameof(name));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        formFields[name] = value;
        return this;
    }

    public IMessageRequestBuilder WithHttpCompletionOption(
        HttpCompletionOption completionOption)
    {
        HttpCompletionOption = completionOption;
        return this;
    }
}