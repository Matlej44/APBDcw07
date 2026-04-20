using APBDcw07.DTOs;
using Microsoft.Data.SqlClient;

namespace APBDcw07.Services;

public class AppointmentService(IConfiguration configuration) : IAppointmentService
{
    private readonly string _connectionString = configuration.GetConnectionString("LocalHostConnection") ??
                                                throw new InvalidOperationException("Connection string not found.");

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
                                                 " A.InternalNotes, " +
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
                CreatedAt = reader.GetDateTime(4)
            };
        }

        return appointmentDetailsDto;
    }

    public async Task<ErrorResponseDto> CreateAppointmentAsync(CreateAppointmentRequestDto appointment)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        if (appointment.appointmentDate < DateTime.Now)
            return Error("Appointment date cannot be in the past.");

        if (await CheckIfDoctorIsInactive(appointment.idDoctor, connection))
            return Error("Doctor is not active.");

        if (await CheckIfPatientIsInactive(appointment.idPatient, connection))
            return Error("Patient is not active.");

        if (await CheckIfDoctorIsBooked(appointment.idDoctor, appointment.appointmentDate, connection))
            return Error("Doctor is already booked for this date.", 409);

        await using var create = new SqlCommand(
            "Insert INTO Appointments(idpatient, iddoctor, appointmentdate, status, reason, createdat) " +
            " VALUES (@idPatient, @idDoctor, @appointmentDate, 'Scheduled', @reason, GETDATE());", connection);
        create.Parameters.AddWithValue("@idPatient", appointment.idPatient);
        create.Parameters.AddWithValue("@idDoctor", appointment.idDoctor);
        create.Parameters.AddWithValue("@appointmentDate", appointment.appointmentDate);
        create.Parameters.AddWithValue("@reason", appointment.reason);
        var executeNonQueryAsync = await create.ExecuteNonQueryAsync();
        return executeNonQueryAsync != 0
            ? new ErrorResponseDto { IsSuccess = true }
            : new ErrorResponseDto { IsSuccess = false };

        ErrorResponseDto Error(string message, int code = 400) =>
            new() { IsSuccess = false, Message = message, StatusCode = code };
    }

    public async Task<ErrorResponseDto> UpdateAppointmentAsync(int id, UpdateAppointmentRequestDto appointment)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        if (await CheckIfAppointmentExists(id, connection))
            return Error("Appointment does not exist.", 404);
        if (await CheckIfDoctorIsInactive(appointment.idDoctor, connection))
            return Error("Doctor is not active.");
        if (await CheckIfPatientIsInactive(appointment.idPatient, connection))
            return Error("Patient is not active.");
        await using var appointmentDate = new SqlCommand("Select AppointmentDate FROM Appointments WHERE IdAppointment=@id;", connection);
        appointmentDate.Parameters.AddWithValue("@id", id);
        var dateTime = (DateTime)(await appointmentDate.ExecuteScalarAsync());
        if (appointment.status == StatusEnum.Completed && dateTime != null &&
            !dateTime.Equals(appointment.appointmentDate))
        {
            return Error("Appointment date cannot be changed once completed.", 409);
        }
            
        await using var update =
            new SqlCommand(
                "Update Appointments SET IdPatient=@idpatient, IdDoctor=@idDoctor, AppointmentDate=@appointmentDate, " +
                " Status=@Status, Reason=@reason, InternalNotes=@notes WHERE IdAppointment=@id;",
                connection);
        update.Parameters.AddWithValue("@idpatient", appointment.idPatient);
        update.Parameters.AddWithValue("@idDoctor", appointment.idDoctor);
        update.Parameters.AddWithValue("@appointmentDate", appointment.appointmentDate);
        update.Parameters.AddWithValue("@Status", appointment.status.ToString());
        update.Parameters.AddWithValue("@reason", appointment.reason);
        update.Parameters.AddWithValue("@notes", (object)appointment.notes?? DBNull.Value);
        update.Parameters.AddWithValue("@id", id);
        var executeNonQueryAsync = await update.ExecuteNonQueryAsync();
        return executeNonQueryAsync == 0 ? Error("Appointment does not exist.", 404) : new ErrorResponseDto { IsSuccess = true };
        ErrorResponseDto Error(string message, int code = 400) =>
            new() { IsSuccess = false, Message = message, StatusCode = code };
    }

    public async Task<ErrorResponseDto> DeleteAppointmentAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        if (await CheckIfAppointmentExists(id, connection))
            return Error("Appointment does not exist.", 404);
        
        var command = new SqlCommand("Select Status FROM Appointments WHERE IdAppointment=@id;", connection);
        command.Parameters.AddWithValue("@id", id);
        var status = (StatusEnum)Enum.Parse(typeof(StatusEnum), (string)await command.ExecuteScalarAsync());
        if (status==StatusEnum.Completed)
            return Error("Appointment is already completed.", 409);
        
        var delete = new SqlCommand("Delete FROM Appointments WHERE IdAppointment=@id;", connection);
        delete.Parameters.AddWithValue("@id", id);
        var executeNonQueryAsync = await delete.ExecuteNonQueryAsync();
        return executeNonQueryAsync == 0 ? Error("Appointment does not exist.", 404) : new ErrorResponseDto { IsSuccess = true };
        ErrorResponseDto Error(string message, int code = 400) =>
            new() { IsSuccess = false, Message = message, StatusCode = code };
    }

    private async Task<bool> CheckIfDoctorIsInactive(int idDoctor, SqlConnection connection)
    {
        await using var isActive =
            new SqlCommand("Select IdDoctor FROM Doctors WHERE IdDoctor=@idDoctor AND IsActive=1;", connection);
        isActive.Parameters.AddWithValue("@idDoctor", idDoctor);
        var executeScalar = isActive.ExecuteScalar();
        return executeScalar is null or DBNull;
    }

    private async Task<bool> CheckIfAppointmentExists(int idAppointment, SqlConnection connection)
    {
        await using var isAppointmentExists =
            new SqlCommand("Select IdAppointment FROM Appointments WHERE IdAppointment=@idAppointment;", connection);
        isAppointmentExists.Parameters.AddWithValue("@idAppointment", idAppointment);
        var executeScalar = isAppointmentExists.ExecuteScalar();
        return executeScalar is null or DBNull;
    }

    private async Task<bool> CheckIfPatientIsInactive(int idPatient, SqlConnection connection)
    {
        await using var isPatientActive =
            new SqlCommand("Select IdPatient FROM Patients WHERE IdPatient=@idPatient AND IsActive=1;", connection);
        isPatientActive.Parameters.AddWithValue("@idPatient", idPatient);
        var executeScalarPatient = isPatientActive.ExecuteScalar();
        return executeScalarPatient is null or DBNull;
    }

    private async Task<bool> CheckIfDoctorIsBooked(int idDoctor, DateTime appointmentDate, SqlConnection connection)
    {
        await using var isDoctorBooked =
            new SqlCommand(
                "Select IdDoctor FROM Appointments WHERE AppointmentDate=@appointment AND IdDoctor=@idDoctor;",
                connection);
        isDoctorBooked.Parameters.AddWithValue("@appointment", appointmentDate);
        isDoctorBooked.Parameters.AddWithValue("@idDoctor", idDoctor);
        var executeScalarBooked = isDoctorBooked.ExecuteScalar();
        return executeScalarBooked is not null or DBNull;
    }
}