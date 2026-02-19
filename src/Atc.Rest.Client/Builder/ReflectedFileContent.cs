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
        object?[]? args = null;

        if (parameters.Length > 0)
        {
            args = new object?[parameters.Length];

            if (parameters.Length == 2 &&
                parameters[0].ParameterType == typeof(long) &&
                parameters[1].ParameterType == typeof(CancellationToken))
            {
                args[0] = long.MaxValue;
                args[1] = CancellationToken.None;
            }
            else
            {
                for (var i = 0; i < parameters.Length; i++)
                {
                    var defaultValue = parameters[i].DefaultValue;
                    args[i] = ReferenceEquals(defaultValue, Missing.Value)
                        ? Type.Missing
                        : defaultValue;
                }
            }
        }

        return (Stream)openReadStreamMethod.Invoke(target, args)!;
    }
}