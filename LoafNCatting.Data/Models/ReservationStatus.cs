using System;
using System.Collections.Generic;

namespace LoafNCatting.Data.Models;

public partial class ReservationStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
