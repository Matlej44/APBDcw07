namespace APBDcw07.DTOs;

public class AppointmentDetailsDto
{
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DoctorLicenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
}