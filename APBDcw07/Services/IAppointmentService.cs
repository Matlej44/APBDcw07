using APBDcw07.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace APBDcw07.Services;

public interface IAppointmentService
{
    Task<List<AppointmentListDto>> GetAllAppointmentsAsync(string? status = null, string? lastName = null);
    Task<AppointmentDetailsDto> GetAppointmentAsync(int id);
    Task<ErrorResponseDto> CreateAppointmentAsync(CreateAppointmentRequestDto appointment);
    Task<ErrorResponseDto> UpdateAppointmentAsync(int id, UpdateAppointmentRequestDto appointment);
    Task<ErrorResponseDto> DeleteAppointmentAsync(int id);
}