using System.Collections.Immutable;
using System.Configuration;
using APBDcw07.DTOs;
using Microsoft.AspNetCore.Mvc;
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
        using var command = new SqlCommand(
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
        var reader = await command.ExecuteReaderAsync();
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
        var command = new SqlCommand("Select P.Email, " +
                                     " P.PhoneNumber, " +
                                     " D.LicenseNumber, " +
                                     " A.InternalNotes, "+
                                     " A.CreatedAt " +
                                     " FROM Appointments A JOIN dbo.Patients P on A.IdPatient = P.IdPatient " +
                                     " JOIN dbo.Doctors D on D.IdDoctor = A.IdDoctor " +
                                     " WHERE A.IdAppointment=@id; ", connection);
        command.Parameters.AddWithValue("@id", id);
        var reader = await command.ExecuteReaderAsync();
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

    public Task<ErrorResponseDto> CreateAppointmentAsync(CreateAppointmentRequestDto appointment)
    {
        throw new NotImplementedException();
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