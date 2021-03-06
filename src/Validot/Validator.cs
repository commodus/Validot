namespace Validot
{
    using System;
    using System.Linq;

    using Validot.Errors;
    using Validot.Factory;
    using Validot.Results;
    using Validot.Settings;
    using Validot.Validation;
    using Validot.Validation.Scheme;
    using Validot.Validation.Stacks;

    public abstract class Validator
    {
        /// <summary>
        /// Gets validator factory - the recommended way of creating instances of <see cref="Validator{T}"/>.
        /// </summary>
        public static ValidatorFactory Factory { get; } = new ValidatorFactory();
    }

    /// <inheritdoc cref="IValidator{T}"/>
    public sealed class Validator<T> : Validator, IValidator<T>
    {
        private readonly IMessageService _messageService;

        private readonly IModelScheme<T> _modelScheme;

        internal Validator(IModelScheme<T> modelScheme, IValidatorSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _modelScheme = modelScheme ?? throw new ArgumentNullException(nameof(modelScheme));

            _messageService = new MessageService(Settings.Translations, _modelScheme.ErrorRegistry, _modelScheme.Template);
            Template = new ValidationResult(_modelScheme.Template.ToDictionary(p => p.Key, p => p.Value.ToList()), _modelScheme.ErrorRegistry, _messageService);
        }

        /// <inheritdoc cref="IValidator{T}.Settings"/>
        public IValidatorSettings Settings { get; }

        /// <inheritdoc cref="IValidator{T}.Template"/>
        public IValidationResult Template { get; }

        /// <inheritdoc cref="IValidator{T}.IsValid"/>
        public bool IsValid(T model)
        {
            var validationContext = new IsValidValidationContext(_modelScheme, Settings.ReferenceLoopProtectionEnabled ? new ReferenceLoopProtectionSettings(model) : null);

            _modelScheme.RootSpecificationScope.Validate(model, validationContext);

            return !validationContext.ErrorFound;
        }

        /// <inheritdoc cref="IValidator{T}.Validate"/>
        public IValidationResult Validate(T model, bool failFast = false)
        {
            var validationContext = new ValidationContext(_modelScheme, failFast, Settings.ReferenceLoopProtectionEnabled ? new ReferenceLoopProtectionSettings(model) : null);

            _modelScheme.RootSpecificationScope.Validate(model, validationContext);

            var isValid = validationContext.Errors is null;

            return isValid
                ? ValidationResult.NoErrorsResult
                : new ValidationResult(validationContext.Errors, _modelScheme.ErrorRegistry, _messageService);
        }
    }
}
