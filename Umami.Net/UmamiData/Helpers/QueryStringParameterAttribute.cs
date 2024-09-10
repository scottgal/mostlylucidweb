namespace Umami.Net.UmamiData.Helpers;

[AttributeUsage(AttributeTargets.Property)]
public class QueryStringParameterAttribute(string name = "", bool isRequired = false) : Attribute
{
    public string Name { get; } = name;

    public bool IsRequired { get; } = isRequired;
}