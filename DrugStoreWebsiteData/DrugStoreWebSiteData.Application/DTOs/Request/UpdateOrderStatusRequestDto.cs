
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Domain.Enums;

namespace DrugStoreWebSiteData.Application.DTOs
{
    public class UpdateOrderStatusRequestDto
    {
        public UpdateOrderStatusRequestDto() { }

        [Required(ErrorMessage = "Order Id is required")]
        public Guid OrderId { get; set; }
        [Required(ErrorMessage = "Order new status is required")]
        public OrderStatus NewStatus { get; set; }
    }
}