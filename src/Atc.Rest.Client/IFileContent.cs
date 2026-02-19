namespace Atc.Rest.Client;

/// <summary>
/// Represents a file to be uploaded via <see cref="Builder.IMessageRequestBuilder.WithBody{TBody}"/>.
/// </summary>
public interface IFileContent
{
    string FileName { get; }

    string? ContentType { get; }

    Stream OpenReadStream();
}