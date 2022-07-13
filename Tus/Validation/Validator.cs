using System.Net;
using tusdotnet.Adapters;

namespace tusdotnet.Validation;

internal sealed class Validator
{
    private readonly Requirement[] _requirements;

    public Validator(params Requirement[] requirements)
    {
        _requirements = requirements ?? new Requirement[0];
    }

    public HttpStatusCode StatusCode { get; private set; }
    public string ErrorMessage { get; private set; }

    public async Task Validate(ContextAdapter context)
    {
        StatusCode = HttpStatusCode.OK;
        ErrorMessage = null;

        foreach (var spec in _requirements)
        {
            spec.Reset();
            await spec.Validate(context);

            if (spec.StatusCode == 0) continue;

            StatusCode = spec.StatusCode;
            ErrorMessage = spec.ErrorMessage;
            break;
        }
    }
}