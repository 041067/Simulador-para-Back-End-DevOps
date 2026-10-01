namespace BackOps.Application.Validators;

using FluentValidation;
using BackOps.Application.Commands;
using BackOps.Domain.Enums;

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.EventDate).GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.TotalCapacity).GreaterThan(0).LessThanOrEqualTo(100000);
        RuleFor(x => x.TicketPrice).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public class CreatePurchaseCommandValidator : AbstractValidator<CreatePurchaseCommand>
{
    public CreatePurchaseCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(10);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}

public class CreateVideoJobCommandValidator : AbstractValidator<CreateVideoJobCommand>
{
    public CreateVideoJobCommandValidator()
    {
        RuleFor(x => x.VideoId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.VideoSizeBytes).GreaterThan(0);
        RuleFor(x => x.DurationSeconds).GreaterThan(0);
        RuleFor(x => x.Operation).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PayerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.PayerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CardToken).NotEmpty().MaximumLength(128);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}

public class InjectFailureCommandValidator : AbstractValidator<InjectFailureCommand>
{
    public InjectFailureCommandValidator()
    {
        RuleFor(x => x.FailureType).IsInEnum();
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1);
        RuleFor(x => x.DurationMs).GreaterThan(0).When(x => x.DurationMs.HasValue);
    }
}

public class ConfigureRateLimitCommandValidator : AbstractValidator<ConfigureRateLimitCommand>
{
    public ConfigureRateLimitCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Limit).GreaterThan(0);
        RuleFor(x => x.Window).GreaterThan(TimeSpan.Zero);
    }
}