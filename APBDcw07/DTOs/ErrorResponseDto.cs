using Microsoft.AspNetCore.Mvc;

namespace APBDcw07.DTOs;

public class ErrorResponseDto
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int StatusCode { get; set; }
}