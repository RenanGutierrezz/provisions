namespace RWMS.Models.Enums;

public enum OrderStatus
{
    Pending,          // Order placed by client, awaiting review
    Accepted,         // Order confirmed by Owner or Manager
    ReadyForDelivery, // Order prepared and ready for client
    Rejected          // Order declined by Owner or Manager (terminal)
}
