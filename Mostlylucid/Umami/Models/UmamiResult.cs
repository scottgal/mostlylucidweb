using System.Net;

namespace Mostlylucid.Umami.Models;

public record UmamiResult<T>(HttpStatusCode Status, string Message, T Data);