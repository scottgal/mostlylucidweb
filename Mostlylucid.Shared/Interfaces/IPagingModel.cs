namespace Mostlylucid.Shared.Interfaces;

public interface IPagingModel<T> where T : class
{
    
    public int Page { get; set; }
    
    public int TotalItems { get; set; }
    
    public int PageSize { get; set; }
    

    public List<T> Data { get; set; }
}