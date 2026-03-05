using System;

namespace DrugStoreWebsiteAuthen.Application.DTOs.Request;

public class RefreshRequestDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }

}
