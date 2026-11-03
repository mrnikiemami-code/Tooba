using FluentValidation;
using Tooba.Shipping.Application.Authoring.Validation;
using Tooba.Shipping.Application.Carriers.Commands;
using Tooba.Shipping.Application.Carriers.Queries;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Domain.Carriers;

namespace Tooba.Shipping.Application.Carriers.Validation;

public static class ShippingCarrierValidationCodes
{
    public const string AccountIdRequired = "shipping.validation.carrier_account_id_required";
    public const string DraftIdRequired = "shipping.validation.carrier_draft_id_required";
    public const string ProviderCodeInvalid = "shipping.validation.carrier_provider_code_invalid";
    public const string DisplayNameInvalid = "shipping.validation.carrier_display_name_invalid";
    public const string MappingCityRequired = "shipping.validation.carrier_mapping_city_required";
    public const string MappingCodeInvalid = "shipping.validation.carrier_mapping_code_invalid";
    public const string BindingAccountRequired = "shipping.validation.carrier_binding_account_required";
    public const string BindingReferenceInvalid = "shipping.validation.carrier_binding_reference_invalid";
    public const string BindingDimensionInvalid = "shipping.validation.carrier_binding_dimension_invalid";
}

public sealed class UpsertShippingCarrierAccountCommandValidator : AbstractValidator<UpsertShippingCarrierAccountCommand>
{
    public UpsertShippingCarrierAccountCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithErrorCode(ShippingValidationCodes.BodyInvalid);
        RuleFor(x => x.Request.ProviderCode)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.ProviderCodeInvalid)
            .MaximumLength(ShippingCarrierAccount.MaxProviderCodeLength)
            .WithErrorCode(ShippingCarrierValidationCodes.ProviderCodeInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.DisplayName)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.DisplayNameInvalid)
            .MaximumLength(ShippingCarrierAccount.MaxDisplayNameLength)
            .WithErrorCode(ShippingCarrierValidationCodes.DisplayNameInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.Environment)
            .IsInEnum()
            .WithErrorCode(ShippingCarrierValidationCodes.ProviderCodeInvalid)
            .When(x => x.Request is not null);
    }
}

public sealed class GetShippingCarrierAccountQueryValidator : AbstractValidator<GetShippingCarrierAccountQuery>
{
    public GetShippingCarrierAccountQueryValidator()
        => RuleFor(x => x.AccountId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
}

public sealed class TestShippingCarrierConnectionCommandValidator : AbstractValidator<TestShippingCarrierConnectionCommand>
{
    public TestShippingCarrierConnectionCommandValidator()
        => RuleFor(x => x.AccountId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
}

public sealed class SyncShippingCarrierReferencesCommandValidator : AbstractValidator<SyncShippingCarrierReferencesCommand>
{
    public SyncShippingCarrierReferencesCommandValidator()
        => RuleFor(x => x.AccountId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
}

public sealed class ListShippingCarrierReferencesQueryValidator : AbstractValidator<ListShippingCarrierReferencesQuery>
{
    public ListShippingCarrierReferencesQueryValidator()
        => RuleFor(x => x.AccountId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
}

public sealed class ListShippingCarrierDestinationMappingsQueryValidator
    : AbstractValidator<ListShippingCarrierDestinationMappingsQuery>
{
    public ListShippingCarrierDestinationMappingsQueryValidator()
        => RuleFor(x => x.AccountId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
}

public sealed class UpsertShippingCarrierDestinationMappingCommandValidator
    : AbstractValidator<UpsertShippingCarrierDestinationMappingCommand>
{
    public UpsertShippingCarrierDestinationMappingCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
        RuleFor(x => x.Request).NotNull().WithErrorCode(ShippingValidationCodes.BodyInvalid);
        RuleFor(x => x.Request.GeographyCityId)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.MappingCityRequired)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.ExternalLocationCode)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.MappingCodeInvalid)
            .MaximumLength(ShippingCarrierDestinationMapping.MaxExternalCodeLength)
            .WithErrorCode(ShippingCarrierValidationCodes.MappingCodeInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.Provenance)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.MappingCodeInvalid)
            .MaximumLength(ShippingCarrierDestinationMapping.MaxProvenanceLength)
            .WithErrorCode(ShippingCarrierValidationCodes.MappingCodeInvalid)
            .When(x => x.Request is not null);
    }
}

public sealed class GetShippingCarrierMethodBindingQueryValidator : AbstractValidator<GetShippingCarrierMethodBindingQuery>
{
    public GetShippingCarrierMethodBindingQueryValidator()
        => RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.DraftIdRequired);
}

public sealed class ReplaceShippingCarrierMethodBindingCommandValidator
    : AbstractValidator<ReplaceShippingCarrierMethodBindingCommand>
{
    public ReplaceShippingCarrierMethodBindingCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.DraftIdRequired);
        RuleFor(x => x.Request).NotNull().WithErrorCode(ShippingValidationCodes.BodyInvalid);
        RuleFor(x => x.Request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedRevisionInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.CarrierAccountId)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.ProviderServiceId)
            .NotEmpty()
            .MaximumLength(ShippingCarrierMethodBinding.MaxIdLength)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingReferenceInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.PaymentTypeId)
            .NotEmpty()
            .MaximumLength(ShippingCarrierMethodBinding.MaxIdLength)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingReferenceInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.PickupTypeId)
            .NotEmpty()
            .MaximumLength(ShippingCarrierMethodBinding.MaxIdLength)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingReferenceInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.DistributionTypeId)
            .NotEmpty()
            .MaximumLength(ShippingCarrierMethodBinding.MaxIdLength)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingReferenceInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.PackingId)
            .NotEmpty()
            .MaximumLength(ShippingCarrierMethodBinding.MaxIdLength)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingReferenceInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.PackageContentId)
            .NotEmpty()
            .MaximumLength(ShippingCarrierMethodBinding.MaxIdLength)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingReferenceInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.PackTypeId)
            .NotEmpty()
            .MaximumLength(ShippingCarrierMethodBinding.MaxIdLength)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingReferenceInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.OriginExternalCityCode)
            .NotEmpty()
            .MaximumLength(ShippingCarrierMethodBinding.MaxIdLength)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingReferenceInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.DefaultLengthCentimeters)
            .GreaterThan(0)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingDimensionInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.DefaultWidthCentimeters)
            .GreaterThan(0)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingDimensionInvalid)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.DefaultHeightCentimeters)
            .GreaterThan(0)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingDimensionInvalid)
            .When(x => x.Request is not null);
    }
}

public sealed class CreateShippingCarrierShipmentCommandValidator
    : AbstractValidator<CreateShippingCarrierShipmentCommand>
{
    public CreateShippingCarrierShipmentCommandValidator()
    {
        RuleFor(x => x.ActorUserId)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired);
        RuleFor(x => x.Request).NotNull().WithErrorCode(ShippingValidationCodes.BodyInvalid);
        RuleFor(x => x.Request.CheckoutId)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired)
            .When(x => x.Request is not null);
        RuleFor(x => x.Request.ShippingGroupId)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired)
            .When(x => x.Request is not null);
    }
}

public sealed class SaveShippingCarrierShipmentSenderCompletionCommandValidator
    : AbstractValidator<SaveShippingCarrierShipmentSenderCompletionCommand>
{
    public SaveShippingCarrierShipmentSenderCompletionCommandValidator()
    {
        RuleFor(x => x.Request.CheckoutId)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
        RuleFor(x => x.Request.ShippingGroupId)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired);
        RuleFor(x => x.Request.FullName)
            .NotEmpty()
            .MaximumLength(256)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired);
        RuleFor(x => x.Request.Mobile)
            .NotEmpty()
            .MaximumLength(32)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired);
        RuleFor(x => x.Request.AddressLine)
            .NotEmpty()
            .MaximumLength(512)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired);
        RuleFor(x => x.Request.PostalCode)
            .NotEmpty()
            .MaximumLength(32)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired);
    }
}

public sealed class ListShippingGroupCarrierExecutionCapabilitiesQueryValidator
    : AbstractValidator<ListShippingGroupCarrierExecutionCapabilitiesQuery>
{
    public ListShippingGroupCarrierExecutionCapabilitiesQueryValidator()
        => RuleFor(x => x.CheckoutId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
}

public sealed class SaveShippingCarrierShipmentPackageCompletionCommandValidator
    : AbstractValidator<SaveShippingCarrierShipmentPackageCompletionCommand>
{
    public SaveShippingCarrierShipmentPackageCompletionCommandValidator()
    {
        RuleFor(x => x.Request.CheckoutId)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
        RuleFor(x => x.Request.ShippingGroupId)
            .NotEmpty()
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired);
        RuleFor(x => x.Request.WeightGrams)
            .GreaterThan(0)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired);
        RuleFor(x => x.Request.LengthCentimeters)
            .GreaterThan(0)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired);
        RuleFor(x => x.Request.WidthCentimeters)
            .GreaterThan(0)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired);
        RuleFor(x => x.Request.HeightCentimeters)
            .GreaterThan(0)
            .WithErrorCode(ShippingCarrierValidationCodes.BindingAccountRequired);
    }
}

public sealed class ListShippingCarrierShipmentsQueryValidator : AbstractValidator<ListShippingCarrierShipmentsQuery>
{
    public ListShippingCarrierShipmentsQueryValidator()
        => RuleFor(x => x.CheckoutId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
}

public sealed class GetShippingCarrierShipmentCommandValidator : AbstractValidator<GetShippingCarrierShipmentCommand>
{
    public GetShippingCarrierShipmentCommandValidator()
        => RuleFor(x => x.OperationId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
}

public sealed class TrackShippingCarrierShipmentCommandValidator : AbstractValidator<TrackShippingCarrierShipmentCommand>
{
    public TrackShippingCarrierShipmentCommandValidator()
        => RuleFor(x => x.OperationId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
}

public sealed class CancelShippingCarrierShipmentCommandValidator : AbstractValidator<CancelShippingCarrierShipmentCommand>
{
    public CancelShippingCarrierShipmentCommandValidator()
        => RuleFor(x => x.OperationId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
}

public sealed class LabelShippingCarrierShipmentCommandValidator : AbstractValidator<LabelShippingCarrierShipmentCommand>
{
    public LabelShippingCarrierShipmentCommandValidator()
        => RuleFor(x => x.OperationId).NotEmpty().WithErrorCode(ShippingCarrierValidationCodes.AccountIdRequired);
}
