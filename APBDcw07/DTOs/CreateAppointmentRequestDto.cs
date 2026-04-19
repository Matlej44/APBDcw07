using System.ComponentModel.DataAnnotations;

namespace APBDcw07.DTOs;

public class CreateAppointmentRequestDto
{
    [Required]
    public int idPatient{get;set;}
    [Required]
    public int idDoctor{get;set;}
    [Required]
    public DateTime appointmentDate{get;set;}
    [Required]
    [MaxLength(250)]
    [MinLength(1)]
    public string reason{get;set;}

}