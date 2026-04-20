using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace APBDcw07.DTOs;

public class UpdateAppointmentRequestDto
{
    [Required]
    public int idPatient{get;set;}
    [Required]
    public int idDoctor{get;set;}
    [Required]
    public DateTime appointmentDate{get;set;}
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusEnum status{get;set;}
    [Required]
    public string reason{get;set;}
    
    public string? notes{get;set;}
    
}