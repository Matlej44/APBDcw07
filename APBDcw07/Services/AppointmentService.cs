using APBDcw07.DTOs;
using Microsoft.Data.SqlClient;

namespace APBDcw07.Services;

public class AppointmentService(IConfiguration configuration) : IAppointmentService
{
    private readonly string _connectionString = configuration.GetConnectionString("LocalHostConnection") ?? throw new InvalidOperationException("Connection string not found.");

    public async Task<List<AppointmentListDto>> GetAllAppointmentsAsync(string? status = null, string? lastName = null)
    {
        var appointmentListDtos = new List<AppointmentListDto>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT " +
            "    a.IdAppointment, " +
            "    a.AppointmentDate, " +
            "    a.Status, " +
            "    a.Reason, " +
            "    p.FirstName + N' ' + p.LastName AS PatientFullName, " +
            "    p.Email AS PatientEmail " +
            "FROM dbo.Appointments a " +
            "JOIN dbo.Patients p ON p.IdPatient = a.IdPatient " +
            "WHERE (@Status IS NULL OR a.Status = @Status) " +
            "  AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName) " +
            "ORDER BY a.AppointmentDate;",
            connection);

        command.Parameters.AddWithValue("@Status", (object)status ?? DBNull.Value);
        command.Parameters.AddWithValue("@PatientLastName", (object)lastName ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync();
        while (reader.Read())
        {
            appointmentListDtos.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5)
            });
            Console.WriteLine(reader.GetString(4));
        }
        return appointmentListDtos;
    }

    public async Task<AppointmentDetailsDto?> GetAppointmentAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        AppointmentDetailsDto? appointmentDetailsDto = null;
        await using var command = new SqlCommand("Select P.Email, " +
                                                 " P.PhoneNumber, " +
                                                 " D.LicenseNumber, " +
                                                 " A.InternalNotes, "+
                                                 " A.CreatedAt " +
                                                 " FROM Appointments A JOIN dbo.Patients P on A.IdPatient = P.IdPatient " +
                                                 " JOIN dbo.Doctors D on D.IdDoctor = A.IdDoctor " +
                                                 " WHERE A.IdAppointment=@id; ", connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();
        while (reader.Read())
        {
            appointmentDetailsDto = new AppointmentDetailsDto()
            {
                Email = reader.GetString(0),
                PhoneNumber = reader.GetString(1),
                DoctorLicenceNumber = reader.GetString(2),
                Notes = reader["InternalNotes"] is DBNull ? string.Empty : reader.GetString(3),
                AppointmentDate = reader.GetDateTime(4)
            };
        }
        return appointmentDetailsDto;
    }

    public async Task<ErrorResponseDto> CreateAppointmentAsync(CreateAppointmentRequestDto appointment)
    {
        var errorResponseDto = new ErrorResponseDto
        {
            IsSuccess = true
        };
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        if (appointment.appointmentDate< DateTime.Now)
        {
            errorResponseDto.IsSuccess = false;
            errorResponseDto.Message = "Appointment date must be in the future.";
            return errorResponseDto;
        }
        
        await using var IsActive = new SqlCommand("Select IdDoctor FROM Doctors WHERE IdDoctor=@idDoctor AND IsActive=1;", connection);
        IsActive.Parameters.AddWithValue("@idDoctor", appointment.idDoctor);
        var executeScalar = IsActive.ExecuteScalar();
        if (executeScalar is null or DBNull)
        {
            errorResponseDto.IsSuccess = false;
            errorResponseDto.Message = "Doctor is not active.";
            return errorResponseDto;
        }
        
        await using var IsPatientActive = new SqlCommand("Select IdPatient FROM Patients WHERE IdPatient=@idPatient AND IsActive=1;", connection);
        IsPatientActive.Parameters.AddWithValue("@idPatient", appointment.idPatient);
        var executeScalarPatient = IsPatientActive.ExecuteScalar();
        if (executeScalarPatient is null or DBNull)
        {
            errorResponseDto.IsSuccess = false;
            errorResponseDto.Message = "Patient is not active.";
            return errorResponseDto;
        }
        
        await using var IsBooked = new SqlCommand("Select IdDoctor FROM Appointments WHERE AppointmentDate=@appointment AND IdDoctor=@idDoctor;", connection);
        IsBooked.Parameters.AddWithValue("@appointment", appointment.appointmentDate);
        IsBooked.Parameters.AddWithValue("@idDoctor", appointment.idDoctor);
        var executeScalarBooked = IsBooked.ExecuteScalar();
        if (executeScalarBooked is not null or DBNull)
        {
            errorResponseDto.IsSuccess = false;
            errorResponseDto.Message = "Doctor is already booked for this date.";
            return errorResponseDto;
        }

        await using var create = new SqlCommand(
            "Insert INTO Appointments(idpatient, iddoctor, appointmentdate, status, reason, createdat) " +
            " VALUES (@idPatient, @idDoctor, @appointmentDate, 'Scheduled', @reason, GETDATE());", connection);
        create.Parameters.AddWithValue("@idPatient", appointment.idPatient);
        create.Parameters.AddWithValue("@idDoctor", appointment.idDoctor);
        create.Parameters.AddWithValue("@appointmentDate", appointment.appointmentDate);
        create.Parameters.AddWithValue("@reason", appointment.reason);
        var executeNonQueryAsync = await create.ExecuteNonQueryAsync();
        if (executeNonQueryAsync != 0) return errorResponseDto;
        errorResponseDto.IsSuccess = false;
        return errorResponseDto;
    }

    public Task<ErrorResponseDto> UpdateAppointmentAsync(int id, UpdateAppointmentRequestDto appointment)
    {
        throw new NotImplementedException();
    }

    public Task<ErrorResponseDto> DeleteAppointmentAsync(int id)
    {
        throw new NotImplementedException();
    }
}