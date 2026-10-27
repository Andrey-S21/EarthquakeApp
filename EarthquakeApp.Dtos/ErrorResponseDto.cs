using System;
using System.Collections.Generic;
using System.Text;

namespace EarthquakeApp.Dtos;

public class ErrorResponseDto
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
}
