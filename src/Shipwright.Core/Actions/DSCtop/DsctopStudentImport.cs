// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Microsoft.Extensions.Configuration;
using Shipwright.Commands;
using Shipwright.Dataflows;
using Shipwright.Dataflows.Sources;
using Shipwright.Dataflows.Transformations;

namespace Shipwright.Actions.DSCtop;

/// <summary>
/// Action for importing students into DSCtop.
/// </summary>
[UsedImplicitly]
public class DsctopStudentImport : IActionFactory
{
    readonly IActionSettingsFactory _actionSettingsFactory;

    public DsctopStudentImport( IActionSettingsFactory actionSettingsFactory )
    {
        _actionSettingsFactory = actionSettingsFactory ?? throw new ArgumentNullException( nameof(actionSettingsFactory) );
    }

    public record Fields
    {
        public string BillingDistrictId { get; } = $"#{nameof(BillingDistrictId)}";
        public string BillingDistrictStateId { get; init; } = nameof(BillingDistrictStateId);
        public string BirthDate { get; init; } = nameof(BirthDate);
        public string CreatedBy { get; } = $"#{nameof(CreatedBy)}";
        public string CreatedAt { get; } = $"#{nameof(CreatedAt)}";
        public string DistrictId { get; } = $"#{nameof(DistrictId)}";
        public string DistrictStudentId { get; init; } = nameof(DistrictStudentId);
        public string Eligible { get; init; } = nameof(Eligible);
        public string EligibleEndDate { get; init; } = nameof(EligibleEndDate);
        public string EligibleStartDate { get; init; } = nameof(EligibleStartDate);
        public string FirstName { get; init; } = nameof(FirstName);
        public string Gender { get; init; } = nameof(Gender);
        public string IepEndDate { get; init; } = nameof(IepEndDate);
        public string IepStartDate { get; init; } = nameof(IepStartDate);
        public string IepStatus { get; init; } = nameof(IepStatus);
        public string LastName { get; init; } = nameof(LastName);
        public string MiddleName { get; init; } = nameof(MiddleName);
        public string ParentalConsent { get; init; } = nameof(ParentalConsent);
        public string ParentalConsentDenied { get; init; } = nameof(ParentalConsentDenied);
        public string ParentalConsentEnd { get; init; } = nameof(ParentalConsentEnd);
        public string ParentalConsentRangeId { get; } = $"#{nameof(ParentalConsentRangeId)}";
        public string ParentalConsentStart { get; init; } = nameof(ParentalConsentStart);
        public string SchoolId { get; } = $"#{nameof(SchoolId)}";
        public string SchoolName { get; init; } = nameof(SchoolName);
        public string SeasSchoolId { get; init; } = nameof(SeasSchoolId);
        public string SeasStudentId { get; init; } = nameof(SeasStudentId);
        public string StateHealthId { get; init; } = nameof(StateHealthId);
        public string StateStudentId { get; init; } = nameof(StateStudentId);
        public string Status { get; init; } = nameof(Status);
        public string StatusEnd { get; init; } = nameof(StatusEnd);
        public string StatusStart { get; init; } = nameof(StatusStart);
        public string StudentId { get; } = $"#{nameof(StudentId)}";
        public string Wheelchair { get; init; } = nameof(Wheelchair);
    }

    public async Task<Command> Create( ActionContext context, CancellationToken cancellationToken )
    {
        if ( context == null ) throw new ArgumentNullException( nameof(context) );

        var actionSettings = _actionSettingsFactory.For<DsctopStudentImport>( context );
        var dsctopSettings = await _actionSettingsFactory.Create<DsctopSettings>( context, actionSettings, cancellationToken );
        var sources = actionSettings.GetCsvSources( context.Tenant );
        var fields = actionSettings.Flatten().GetSection( "fields" ).Get<Fields>() ?? new();
        var keys = actionSettings.GetValue( "keys", Array.Empty<string>() );

        // default iep start date is always the most recent August 1.
        var iepStartDateDefault = new DateTime( DateTime.Today.Year, 8, 1 )
            .AddYears( DateTime.Today.Month < 8 ? -1 : 0 );

        // default status start/end dates
        var statusStartDateDefault = DateTime.Today.AddYears( -1 );
        var statusEndDateDefault = statusStartDateDefault.AddYears( 20 ).AddDays( -1 );

        return new Dataflow
        {
            Name = $"{dsctopSettings.ProductName} Student Import",
            Sources = sources.ToList<Source>(),
            Keys = keys,
            Configuration = actionSettings,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            Transformations =
            {
                // required fields
                new Required { Fields = { fields.FirstName, fields.LastName, fields.BirthDate } },

                // defaults that apply to all records
                new DefaultValue
                {
                    Defaults =
                    {
                        new( fields.CreatedAt, () => DateTime.UtcNow ),
                        new( fields.CreatedBy, () => dsctopSettings.ImportUser ),
                        new( fields.DistrictId, () => dsctopSettings.DistrictId ),
                        new( fields.Eligible, () => dsctopSettings.DistrictEligibleDefault ),
                        new( fields.IepStartDate, () => iepStartDateDefault ),
                        new( fields.IepStatus, () => "1" ),
                        new( fields.Status, () => "1" ),
                    }
                },

                // field length limits
                new Truncate
                {
                    Fields =
                    {
                        new( fields.FirstName, 30 ),
                        new( fields.MiddleName, 30 ),
                        new( fields.LastName, 30 )
                    }
                },

                // casing convention
                new Conversion
                {
                    Converter = Conversion.ToUpperCase,
                    Fields =
                    {
                        fields.FirstName,
                        fields.LastName,
                        fields.MiddleName,
                        fields.SchoolName
                    }
                },

                // date conversions
                new Conversion
                {
                    Converter = Conversion.ToDate,
                    Fields =
                    {
                        fields.BirthDate,
                        fields.IepEndDate,
                        fields.IepStartDate,
                        fields.ParentalConsentEnd,
                        fields.ParentalConsentStart,
                        fields.StatusEnd,
                        fields.StatusStart
                    }
                },

                // boolean conversions
                // note: these are converted to 1 and 0 string literals
                new Conversion
                {
                    Converter = ( object value, out object? result ) =>
                    {
                        var converted = Conversion.ToBoolean( value, out result );

                        if ( converted && result is bool flag )
                            result = flag ? "1" : "0";

                        return converted;
                    },
                    Fields =
                    {
                        fields.IepStatus,
                        fields.Eligible,
                        fields.ParentalConsent,
                        fields.ParentalConsentDenied,
                        fields.Status,
                        fields.Wheelchair
                    }
                },

                // set parental consent to false when parental consent is explicitly denied
                // this will never turn parental consent ON; it can only turn it OFF
                new Code
                {
                    Delegate = ( record, ct ) =>
                    {
                        if ( record.TryGetValue( fields.ParentalConsentDenied, out var value ) && value is bool denied )
                        {
                            record[fields.ParentalConsent] = "0";
                        }

                        return Task.CompletedTask;
                    }
                },

                // calculate default iep end date when not provided
                // should be one year less one day from the start date (which may be the default)
                new Code
                {
                    Delegate = ( record, ct ) =>
                    {
                        if
                        (
                            !record.TryGetValue( fields.IepEndDate, out _ ) &&
                            record.TryGetValue( fields.IepStartDate, out var value ) &&
                            value is DateTime start
                        )
                        {
                            record[fields.IepEndDate] = start.AddYears( 1 ).AddDays( -1 );
                        }

                        return Task.CompletedTask;
                    }
                }
            }
        };
    }
}
