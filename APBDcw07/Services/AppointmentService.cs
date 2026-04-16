using APBDcw07.DTOs;

namespace APBDcw07.Services;

public class AppointmentService : IAppointmentService
{
    public Task<List<AppointmentListDto>> GetAllAppointmentsAsync()
    {
        
        return Task.FromResult(new List<AppointmentListDto>());
    }
}