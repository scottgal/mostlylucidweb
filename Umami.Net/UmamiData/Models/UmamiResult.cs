using System.Net;

namespace Umami.Net.UmamiData.Models;

public record UmamiResult<T>(HttpStatusCode Status, string Message, T Data);