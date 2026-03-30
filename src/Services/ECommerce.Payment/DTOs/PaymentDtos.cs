using Microsoft.VisualBasic;

namespace ECommerce.Payment.DTOs
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public bool Success { get; set; }
        public string? ChargeId { get; set; }
        public string? FailurReason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
