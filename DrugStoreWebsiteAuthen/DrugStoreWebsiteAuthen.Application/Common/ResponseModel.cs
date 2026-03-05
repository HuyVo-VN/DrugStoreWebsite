namespace DrugStoreWebsiteAuthen.Application.Common;

public class ResponseModel<T>
{
    public int Status { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }

}
