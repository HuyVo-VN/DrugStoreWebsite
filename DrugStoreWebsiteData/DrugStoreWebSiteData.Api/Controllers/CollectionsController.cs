using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrugStoreWebSiteData.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollectionsController : ControllerBase
    {
        private readonly ICollectionService _collectionService;

        public CollectionsController(ICollectionService collectionService)
        {
            _collectionService = collectionService;
        }

        [AllowAnonymous]
        [HttpGet("homepage")]
        public async Task<IActionResult> GetHomepageCollections([FromQuery] int limit = 5)
        {
            var result = await _collectionService.GetHomepageCollectionsAsync(limit);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllCollectionsAdmin()
        {
            var result = await _collectionService.GetAllCollectionsAdminAsync();
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequestDto request)
        {
            var result = await _collectionService.CreateCollectionAsync(request);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCollection(Guid id, [FromBody] UpdateCollectionRequestDto request)
        {
            var result = await _collectionService.UpdateCollectionAsync(id, request);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCollection(Guid id)
        {
            var result = await _collectionService.DeleteCollectionAsync(id);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
    }
}