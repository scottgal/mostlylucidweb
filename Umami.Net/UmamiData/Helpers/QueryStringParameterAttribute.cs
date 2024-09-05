namespace Umami.Net.UmamiData.Helpers;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class QueryStringParameterAttribute(string name = "",  bool isRequired=false) : Attribute
{
    public string Name { get; } = name;
    
    public bool IsRequired { get; } = isRequired;
    
}