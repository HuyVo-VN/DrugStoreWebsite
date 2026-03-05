namespace DrugStoreWebsiteAuthen.Application.Common;

public enum ResultStatus
{
    Success = 200,
    NoContent = 204,
    Created= 201,
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    InternalError = 500
}
