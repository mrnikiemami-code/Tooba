==== Tooba.Identity.Contracts ====
ActorContactContracts.cs
ActorIdentifierResolverContracts.cs
Auth/IdentityAuthenticationContracts.cs
Auth/IdentityPrimitiveContracts.cs
Auth/OtpDeliveryContracts.cs
Problems/IdentityDuplicateIdentifierFault.cs
Problems/IdentityErrorCatalogContributor.cs
Problems/IdentityErrorCodes.cs
Problems/IdentityErrorResourceSet.cs
Resources/IdentityErrors.resx
Tooba.Identity.Contracts.csproj

==== Tooba.Identity.Domain ====
IdentityDomain.cs
Tooba.Identity.Domain.csproj

==== Tooba.Identity.Application ====
IdentityContracts.cs
Tooba.Identity.Application.csproj

==== Tooba.Identity.Infrastructure ====
Adapters/ActorContactLookupAdapter.cs
Adapters/ActorIdentifierResolverAdapter.cs
Authentication/IdentityAuthenticationService.cs
Contacts/EfIdentityContactLookup.cs
Events/IdentityEvents.cs
ExternalIdentity/EfExternalIdentityDirectory.cs
IdentityModule.cs
IdentityOutboxRegistration.cs
Mfa/EfMfaEnrollmentStore.cs
Otp/CapturingOtpDeliveryProvider.cs
Otp/CapturingOtpSender.cs
Otp/FailClosedOtpDeliveryProvider.cs
Otp/IdentityOtpLoginService.cs
Otp/InMemoryOtpChallengeService.cs
Otp/OtpDeliveryInstrumentation.cs
Otp/OtpDeliveryOptions.cs
Otp/OtpDeliveryProviderSender.cs
Otp/WebhookOtpDeliveryProvider.cs
PasswordHashing/AspNetPasswordHashingService.cs
Persistence/IdentityDbContext.cs
Persistence/Migrations/20260823023657_InitialIdentity.cs
Persistence/Migrations/20260823023657_InitialIdentity.Designer.cs
Persistence/Migrations/20260823064756_SessionCredentialLifecycle.cs
Persistence/Migrations/20260823064756_SessionCredentialLifecycle.Designer.cs
Persistence/Migrations/IdentityDbContextModelSnapshot.cs
SecurityEvents/InMemoryIdentitySecurityEventSink.cs
Sessions/IdentityLifecycleService.cs
Tooba.Identity.Infrastructure.csproj

