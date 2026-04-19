using APBDcw07.DTOs;
using APBDcw07.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBDcw07.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllAppointments([FromQuery] string? status = null, [FromQuery] string? patientLastName = null)
        {
            var appointments = await appointmentService.GetAllAppointmentsAsync(status, patientLastName);
            return Ok(appointments);
        }
        
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IActionResult> GetAppointment(int id)
        {
            var appointment = await appointmentService.GetAppointmentAsync(id);
            if (appointment == null) return NotFound();
            return Ok(appointment);
        }
        [HttpPost]
        public async Task<IActionResult> CreateAppointment(CreateAppointmentRequestDto appointment)
        {
            var appointmentDetails = await appointmentService.CreateAppointmentAsync(appointment);
            if (!appointmentDetails.IsSuccess) return BadRequest(appointmentDetails.Message);
            return Created();
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> UpdateAppointment(int id, UpdateAppointmentRequestDto appointment)
        {
            var appointmentDetails = await appointmentService.UpdateAppointmentAsync(id, appointment);
            return Ok();
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appointmentDetails = await appointmentService.DeleteAppointmentAsync(id);
            return Ok();
        }

    }
}
