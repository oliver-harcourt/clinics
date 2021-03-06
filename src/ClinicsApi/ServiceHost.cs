﻿using System;
using System.Reflection;
using Api.Common;
using Api.Common.Validators;
using ApplicationServices;
using AppointmentsApplication;
using AppointmentsApplication.Storage;
using AppointmentsDomain;
using AppointmentsStorage;
using ClinicsApplication;
using ClinicsApplication.Storage;
using ClinicsDomain;
using ClinicsStorage;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Funq;
using InfrastructureServices.ApplicationServices;
using InfrastructureServices.Eventing.Notifications;
using InfrastructureServices.Eventing.ReadModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PaymentsApplication;
using PaymentsApplication.Storage;
using PaymentsDomain;
using PaymentsStorage;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Validation;
using Storage;
using Storage.Interfaces;
using Storage.Interfaces.ReadModels;
using IRepository = Storage.IRepository;

namespace ClinicsApi
{
    public class ServiceHost : AppHostBase
    {
        private static readonly Assembly[] AssembliesContainingServicesAndDependencies = {typeof(Startup).Assembly};
        public static readonly Assembly[] AssembliesContainingDomainEntities =
        {
            typeof(EntityEvent).Assembly,
            typeof(ClinicEntity).Assembly,
            typeof(AppointmentEntity).Assembly,
            typeof(PaymentEntity).Assembly
        };
        private static IRepository repository;
        private IChangeEventNotificationSubscription changeEventNotificationSubscription;
        private IReadModelProjectionSubscription readModelProjectionSubscription;

        public ServiceHost() : base("Clinics", AssembliesContainingServicesAndDependencies)
        {
        }

        public override void Configure(Container container)
        {
            var debugEnabled = AppSettings.Get(nameof(HostConfig.DebugMode), false);
            this.ConfigureServiceHost(debugEnabled);

            RegisterValidators(container);
            RegisterDependencies(container);
        }

        private static void RegisterDependencies(Container container)
        {
            static IRepository ResolveRepository(Container c)
            {
                return repository ??=
                    LocalMachineFileRepository.FromAppSettings(c.Resolve<IAppSettings>());
            }

            container.AddSingleton<ILogger>(c => new Logger<ServiceHost>(new NullLoggerFactory()));
            container.AddSingleton<IDependencyContainer>(new FuncDependencyContainer(container));
            container.AddSingleton<IIdentifierFactory, ClinicIdentifierFactory>();
            container.AddSingleton<IChangeEventMigrator>(c => new ChangeEventTypeMigrator());
            container.AddSingleton<IDomainFactory>(c => DomainFactory.CreateRegistered(
                c.Resolve<IDependencyContainer>(), AssembliesContainingDomainEntities));

            container.AddSingleton<IEventStreamStorage<ClinicEntity>>(c =>
                new GeneralEventStreamStorage<ClinicEntity>(c.Resolve<ILogger>(), c.Resolve<IDomainFactory>(),
                    c.Resolve<IChangeEventMigrator>(),
                    ResolveRepository(c)));
            container.AddSingleton<IClinicStorage>(c =>
                new ClinicStorage(c.Resolve<ILogger>(), c.Resolve<IDomainFactory>(),
                    c.Resolve<IEventStreamStorage<ClinicEntity>>(), ResolveRepository(c)));
            container.AddSingleton<IClinicsApplication, ClinicsApplication.ClinicsApplication>();
            container.AddSingleton<IPersonsService>(c =>
                new PersonsServiceClient(c.Resolve<IAppSettings>().GetString("PersonsApiBaseUrl")));

            container.AddSingleton<IEventStreamStorage<AppointmentEntity>>(c =>
                new GeneralEventStreamStorage<AppointmentEntity>(c.Resolve<ILogger>(), c.Resolve<IDomainFactory>(),
                    c.Resolve<IChangeEventMigrator>(),
                    ResolveRepository(c)));
            container.AddSingleton<IAppointmentStorage>(c =>
                new AppointmentStorage(c.Resolve<ILogger>(), c.Resolve<IDomainFactory>(),
                    c.Resolve<IEventStreamStorage<AppointmentEntity>>(), ResolveRepository(c)));
            container.AddSingleton<IAppointmentsApplication, AppointmentsApplication.AppointmentsApplication>();
            container.AddSingleton<IClinicsService>(c =>
                new ClinicsInProcessService(c.Resolve<IClinicsApplication>()));

            container.AddSingleton<IEventStreamStorage<PaymentEntity>>(c =>
                new GeneralEventStreamStorage<PaymentEntity>(c.Resolve<ILogger>(), c.Resolve<IDomainFactory>(),
                    c.Resolve<IChangeEventMigrator>(),
                    ResolveRepository(c)));
            container.AddSingleton<IPaymentStorage>(c =>
                new PaymentStorage(c.Resolve<ILogger>(), c.Resolve<IDomainFactory>(),
                    c.Resolve<IEventStreamStorage<PaymentEntity>>(), ResolveRepository(c)));
            container.AddSingleton<IPaymentsApplication, PaymentsApplication.PaymentsApplication>();

            container.AddSingleton<IReadModelProjectionSubscription>(c => new InProcessReadModelProjectionSubscription(
                c.Resolve<ILogger>(), c.Resolve<IIdentifierFactory>(), c.Resolve<IChangeEventMigrator>(),
                c.Resolve<IDomainFactory>(), ResolveRepository(c),
                new IReadModelProjection[]
                {
                    new ClinicEntityReadModelProjection(c.Resolve<ILogger>(), ResolveRepository(c)),
                    new PaymentEntityReadModelProjection(c.Resolve<ILogger>(), ResolveRepository(c))
                },
                c.Resolve<IEventStreamStorage<ClinicEntity>>(),
                c.Resolve<IEventStreamStorage<PaymentEntity>>()));

            container.AddSingleton<IChangeEventNotificationSubscription>(c =>
                new InProcessChangeEventNotificationSubscription(
                    c.Resolve<ILogger>(), c.Resolve<IChangeEventMigrator>(),
                    new[]
                    {
                        new DomainEventPublisherSubscriberPair(new PersonDomainEventPublisher(),
                            new ClinicManagerEventSubscriber(c.Resolve<IClinicsApplication>())),
                        new DomainEventPublisherSubscriberPair(new AppointmentDomainEventPublisher(),
                            new PaymentManagerEventSubscriber(c.Resolve<IPaymentsApplication>()))
                    },
                    c.Resolve<IEventStreamStorage<ClinicEntity>>(),
                    c.Resolve<IEventStreamStorage<AppointmentEntity>>()));
        }

        private void RegisterValidators(Container container)
        {
            Plugins.Add(new ValidationFeature());
            container.RegisterValidators(AssembliesContainingServicesAndDependencies);
            container.AddSingleton<IHasSearchOptionsValidator, HasSearchOptionsValidator>();
            container.AddSingleton<IHasGetOptionsValidator, HasGetOptionsValidator>();
        }

        public override void OnAfterInit()
        {
            base.OnAfterInit();

            this.readModelProjectionSubscription = Container.Resolve<IReadModelProjectionSubscription>();
            this.readModelProjectionSubscription.Start();
            this.changeEventNotificationSubscription = Container.Resolve<IChangeEventNotificationSubscription>();
            this.changeEventNotificationSubscription.Start();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            (this.readModelProjectionSubscription as IDisposable)?.Dispose();
            (this.changeEventNotificationSubscription as IDisposable)?.Dispose();
        }
    }
}