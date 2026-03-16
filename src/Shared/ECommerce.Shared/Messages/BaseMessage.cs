using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Shared.Message
{
    /// <summary>
    /// Base record for ALL RabbitMQ event messages in this system.
    /// Every event (OrderPlaced, PaymentReceived, etc.) inherits from this.
    /// Never create this directly — always use a specific event class.
    /// </summary>
    public abstract record BaseMessage
    {
        /// <summary>
        /// Unique ID for this specific message instance.
        /// 
        /// PURPOSE: Idempotency — when RabbitMQ delivers the same message
        /// twice (network hiccup, consumer restart), the consumer checks:
        /// "have I already processed this MessageId?"
        /// If yes → skip it. This prevents double-processing.
        /// 
        /// Guid = Globally Unique Identifier
        /// Example: 3f2504e0-4f89-11d3-9a0c-0305e82c3301
        /// 
        /// init = can only be set once at object creation, never changed after.
        /// Events must be immutable — once created, never modified.
        /// 
        /// Guid.NewGuid() = auto-generates a unique ID every time.
        /// You never set this manually — it just works.
        /// </summary>
        public Guid MessageId { get; init; }

        /// <summary>
        /// OpenTelemetry Trace ID — for distributed tracing across async boundaries.
        /// 
        /// PROBLEM it solves:
        /// HTTP calls: OpenTelemetry carries TraceId automatically via headers.
        /// RabbitMQ messages: OpenTelemetry CANNOT carry TraceId automatically.
        /// So we embed it manually in every message payload — this field.
        /// 
        /// FLOW:
        /// Gateway generates TraceId for every request
        ///   → flows through HTTP headers automatically
        ///   → when Order Service publishes to RabbitMQ, it copies TraceId here
        ///   → Notification Service reads it from here and continues the trace
        /// 
        /// Without this: your trace breaks at RabbitMQ.
        /// You cannot see the full request journey in Jaeger.
        /// 
        /// string.Empty instead of null = safer default.
        /// null causes NullReferenceException if someone forgets to check.
        /// </summary>
        public string TraceId { get; init; } = string.Empty;

        /// <summary>
        /// Multi-tenant identifier — which tenant (customer/company) this event belongs to.
        /// 
        /// WHY THIS MATTERS:
        /// This system may serve multiple companies (tenants).
        /// Every DB query in every service MUST filter by TenantId.
        /// 
        /// Example:
        ///   Tenant A places an order → TenantId = "tenant_acme"
        ///   Tenant B places an order → TenantId = "tenant_xyz"
        ///   Inventory Service must only reduce stock for the correct tenant.
        /// 
        /// If you forget to filter by TenantId in even ONE query,
        /// Tenant A can see Tenant B's data — a critical security bug.
        /// 
        /// This field carries TenantId across the async (RabbitMQ) boundary
        /// just like TraceId does.
        /// </summary>
        public string TenantId { get; init; } = string.Empty;

        /// <summary>
        /// When this event actually happened — always in UTC.
        /// 
        /// WHY UTC:
        /// Services may run in different timezones (India, US, Europe).
        /// UTC is the universal standard — always consistent.
        /// Never use DateTime.Now (local time) in distributed systems.
        /// 
        /// WHY NOT "ProcessedAt":
        /// This records when the event OCCURRED, not when it was processed.
        /// Consumers may process events with delay (queue backlog).
        /// OccurredAt lets consumers reason about the correct sequence of events.
        /// 
        /// Example:
        ///   OccurredAt = 10:00:00 UTC (when order was placed)
        ///   Processed  = 10:00:05 UTC (5 seconds later by Notification Service)
        ///   The 5 second gap is normal — OccurredAt is what matters for business logic.
        /// </summary>
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }
}
