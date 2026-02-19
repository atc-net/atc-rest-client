namespace Atc.Rest.Client.Builder;

internal sealed class ReflectedFileContent : IFileContent
{
    private readonly object target;
    private readonly PropertyInfo fileNameProp;
    private readonly PropertyInfo? contentTypeProp;
    private readonly MethodInfo openReadStreamMethod;

    public ReflectedFileContent(
        object target,
        PropertyInfo fileNameProp,
        PropertyInfo? contentTypeProp,
        MethodInfo openReadStreamMethod)
    {
        this.target = target;
        this.fileNameProp = fileNameProp;
        this.contentTypeProp = contentTypeProp;
        this.openReadStreamMethod = openReadStreamMethod;
    }

    public string FileName => (string)fileNameProp.GetValue(target)!;

    public string? ContentType => (string?)contentTypeProp?.GetValue(target);

    public Stream OpenReadStream()
    {
        var parameters = openReadStreamMethod.GetParameters();
        var args = parameters.Length > 0
            ? parameters.Select(p => p.DefaultValue).ToArray()
            : null;

        return (Stream)openReadStreamMethod.Invoke(target, args)!;
    }
}