using System;
using System.Collections.Generic;

namespace BookingApp.Models;

public partial class Booking
{
    public int Id { get; set; }

    public string Topic { get; set; } = null!;

    public DateOnly Date { get; set; }

    public string Info { get; set; } = null!;

    public string FromTime { get; set; } = null!;

    public string ToTime { get; set; } = null!;

    public int Room { get; set; }

    public string Status { get; set; } = null!;

    public string BookingType { get; set; } = null!;

    public string? SerialId { get; set; }

    public string User { get; set; } = null!;
}
