using APBDcw07.DTOs;

namespace APBDcw07.Services;

public interface IAppointmentService
{
    Task<List<AppointmentListDto>> GetAllAppointmentsAsync();
}