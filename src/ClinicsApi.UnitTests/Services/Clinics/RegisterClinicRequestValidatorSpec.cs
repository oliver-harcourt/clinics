﻿using Api.Interfaces.ServiceOperations.Clinics;
using ClinicsApi.Properties;
using ClinicsApi.Services.Clinics;
using ClinicsDomain;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ServiceStack.FluentValidation;

namespace ClinicsApi.UnitTests.Services.Clinics
{
    [TestClass, TestCategory("Unit")]
    public class RegisterClinicRequestValidatorSpec
    {
        private RegisterClinicRequest dto;
        private Mock<IIdentifierFactory> identifierFactory;
        private RegisterClinicRequestValidator validator;

        [TestInitialize]
        public void Initialize()
        {
            this.identifierFactory = new Mock<IIdentifierFactory>();
            this.identifierFactory.Setup(f => f.IsValid(It.IsAny<Identifier>())).Returns(true);
            this.validator = new RegisterClinicRequestValidator(this.identifierFactory.Object);
            this.dto = new RegisterClinicRequest
            {
                Id = "anid",
                Jurisdiction = ClinicLicense.Jurisdictions[0],
                CertificateNumber = "ABC123"
            };
        }

        [TestMethod]
        public void WhenAllProperties_ThenSucceeds()
        {
            this.validator.ValidateAndThrow(this.dto);
        }

        [TestMethod]
        public void WhenJurisdictionIsNull_ThenThrows()
        {
            this.dto.Jurisdiction = null;

            this.validator
                .Invoking(x => x.ValidateAndThrow(this.dto))
                .Should().Throw<ValidationException>()
                .WithValidationMessageForNotEmpty();
        }

        [TestMethod]
        public void WhenJurisdictionNotValid_ThenThrows()
        {
            this.dto.Jurisdiction = "invalid";

            this.validator
                .Invoking(x => x.ValidateAndThrow(this.dto))
                .Should().Throw<ValidationException>()
                .WithValidationMessageLike(Resources.RegisterClinicRequestValidator_InvalidJurisdiction);
        }

        [TestMethod]
        public void WhenJurisdiction_ThenSucceeds()
        {
            this.dto.Jurisdiction = ClinicLicense.Jurisdictions[0];

            this.validator.ValidateAndThrow(this.dto);
        }

        [TestMethod]
        public void WhenNumberIsNull_ThenThrows()
        {
            this.dto.CertificateNumber = null;

            this.validator
                .Invoking(x => x.ValidateAndThrow(this.dto))
                .Should().Throw<ValidationException>()
                .WithValidationMessageForNotEmpty();
        }

        [TestMethod]
        public void WhenNumberNotValid_ThenThrows()
        {
            this.dto.CertificateNumber = "##aninvalidnumber##";

            this.validator
                .Invoking(x => x.ValidateAndThrow(this.dto))
                .Should().Throw<ValidationException>()
                .WithValidationMessageLike(Resources.RegisterClinicRequestValidator_InvalidNumber);
        }

        [TestMethod]
        public void WhenNumber_ThenSucceeds()
        {
            this.dto.CertificateNumber = "ABC123";

            this.validator.ValidateAndThrow(this.dto);
        }
    }
}