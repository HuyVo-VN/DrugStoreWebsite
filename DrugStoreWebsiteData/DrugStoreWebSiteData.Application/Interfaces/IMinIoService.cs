using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrugStoreWebSiteData.Application.Interfaces
{
    public interface IMinIoService
    {
        Task<string> UploadFileAsync(byte[] fileData, string fileName, string contentType);
    }
}
