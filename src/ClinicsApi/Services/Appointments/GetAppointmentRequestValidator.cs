using Api.Common.Validators;
using Api.Interfaces.ServiceOperations.Appointments;
using ClinicsApi.Properties;
using Domain.Interfaces.Entities;
using ServiceStack.FluentValidation;

namespace ClinicsApi.Services.Appointments
{
    public class GetAppointmentRequestValidator : AbstractValidator<GetAppointmentRequest>
    {
        public GetAppointmentRequestValidator(IIdentifierFactory identifierFactory)
        {
            RuleFor(dto => dto.Id).IsEntityId(identifierFactory)
                .WithMessage(Resources.AnyValidator_InvalidId);
        }
    }
}