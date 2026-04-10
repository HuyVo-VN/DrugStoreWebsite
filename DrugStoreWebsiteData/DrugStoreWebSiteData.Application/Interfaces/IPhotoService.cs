using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DrugStoreWebSiteData.Application.Interfaces
{
    public interface IPhotoService
    {
        Task<string> AddPhotoAsync(IFormFile file);
        Task<bool> DeletePhotoAsync(string imageUrl);
    }
}
