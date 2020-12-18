using ServiceStack;

namespace Api.Interfaces.ServiceOperations.Appointments
{
    [Route("/appointments/{Id}", "GET")]
    public class GetAppointmentRequest : GetOperation<GetAppointmentResponse>
    {
        public string Id { get; set; }
    }
}