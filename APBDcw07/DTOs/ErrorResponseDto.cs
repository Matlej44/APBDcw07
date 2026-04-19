using Microsoft.AspNetCore.Mvc;

namespace APBDcw07.DTOs;

public class ErrorResponseDto
{
    IActionResult? Error { get; set; }
}