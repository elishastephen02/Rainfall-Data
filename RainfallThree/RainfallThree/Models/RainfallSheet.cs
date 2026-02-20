using System;
using System.Collections.Generic;

namespace RainfallThree.Models;

public partial class RainfallSheet
{
    public int Index { get; set; }

    public byte Latdeg { get; set; }

    public byte Latmin { get; set; }

    public byte Longdeg { get; set; }

    public byte Longmin { get; set; }

    public short ReturnPeriod { get; set; }

    public double? _5Min { get; set; }

    public double? _10Min { get; set; }

    public double? _15Min { get; set; }

    public double? _30Min { get; set; }

    public double? _60Min { get; set; }

    public double? _120Min { get; set; }

    public double? _1440Min { get; set; }

    public double? _4320Min { get; set; }

    public double? _10080Min { get; set; }

    public short SourceSheet { get; set; }
}
