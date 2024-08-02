using Cfo.Cats.Application.Common.Validators;
using Cfo.Cats.Domain.Entities.Participants;

namespace Cfo.Cats.Application.Features.Participants.DTOs;

[Description("Risk")]
public class RiskDto
{
    [Description("Activity Recommendations")]
    public string? ActivityRecommendations { get; set; }

    [Description("Date Latest Activity Recommendations Received")]
    public DateTime? ActivityRecommendationsReceived { get; set; }

    [Description("Activity Restrictions")]
    public string? ActivityRestrictions { get; set; }

    [Description("Date Latest Activity Restrictions Received")]
    public DateTime? ActivityRestrictionsReceived { get; set; }

    [Description("Additional Information")]
    public string? AdditionalInformation { get; set; }

    [Description("License Conditions")]
    public string? LicenseConditions { get; set; }

    [Description("License/Supervision End Date")]
    public DateTime? LicenseEnd { get; set; }

    [Description("PSF Restrictions")]
    public string? PSFRestrictions { get; set; }

    [Description("Date Latest PSF Restrictions Received")]
    public DateTime? PSFRestrictionsReceived { get; set; }

    public RiskDetail CustodyRiskDetail { get; set; } = new();
    public RiskDetail CommunityRiskDetail { get; set; } = new();

    [Description("Custody")]
    public bool IsRelevantToCustody { get; set; } = false;

    [Description("Community")]
    public bool IsRelevantToCommunity { get; set; } = false;

    [Description("Sexual Harm Prevention Order (SHPO)")]
    public ConfirmationStatus? IsSubjectToSHPO { get; set; }

    [Description("NSD case")]
    public ConfirmationStatus? NSDCase { get; set; }

    [Description("Specific Risk(s)")]
    public string? SpecificRisk { get; set; }

    public MappaCategory? MappaCategory { get; set; }
    public MappaLevel? MappaLevel { get; set; }

    [Description("Referrer Name")]
    public string? ReferrerName { get; set; }

    [Description("Referrer Email")]
    public string? ReferrerEmail { get; set; }

    [Description("Referred on")]
    public DateTime? ReferredOn { get; set; }

    [Description("Declaration")]
    public bool DeclarationSigned { get; set; } = false;

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Risk, RiskDto>(MemberList.None)
                .ForMember(dest => dest.CommunityRiskDetail, opt => opt.MapFrom(src => new RiskDetail
                {
                    RiskToChildren = src.RiskToChildrenInCommunity,
                    RiskToPublic = src.RiskToPublicInCommunity,
                    RiskToKnownAdult = src.RiskToKnownAdultInCommunity,
                    RiskToStaff = src.RiskToStaffInCommunity,
                    RiskToSelf = src.RiskToSelfInCommunity,
                    RiskToOtherPrisoners = src.RiskToOtherPrisonersInCommunity,
                }))
                .ForMember(dest => dest.CustodyRiskDetail, opt => opt.MapFrom(src => new RiskDetail
                {
                    RiskToChildren = src.RiskToChildrenInCustody,
                    RiskToPublic = src.RiskToPublicInCustody,
                    RiskToKnownAdult = src.RiskToKnownAdultInCustody,
                    RiskToStaff = src.RiskToStaffInCustody,
                    RiskToSelf = src.RiskToSelfInCustody,
                    RiskToOtherPrisoners = src.RiskToOtherPrisonersInCustody,
                }))
                .ReverseMap()
                .ForPath(src => src.RiskToChildrenInCommunity, opt => opt.MapFrom(dest => dest.CommunityRiskDetail.RiskToChildren))
                .ForPath(src => src.RiskToPublicInCommunity, opt => opt.MapFrom(dest => dest.CommunityRiskDetail.RiskToPublic))
                .ForPath(src => src.RiskToKnownAdultInCommunity, opt => opt.MapFrom(dest => dest.CommunityRiskDetail.RiskToKnownAdult))
                .ForPath(src => src.RiskToStaffInCommunity, opt => opt.MapFrom(dest => dest.CommunityRiskDetail.RiskToStaff))
                .ForPath(src => src.RiskToSelfInCommunity, opt => opt.MapFrom(dest => dest.CommunityRiskDetail.RiskToSelf))
                .ForPath(src => src.RiskToOtherPrisonersInCommunity, opt => opt.MapFrom(dest => dest.CommunityRiskDetail.RiskToOtherPrisoners))
                .ForPath(src => src.RiskToChildrenInCustody, opt => opt.MapFrom(dest => dest.CustodyRiskDetail.RiskToChildren))
                .ForPath(src => src.RiskToPublicInCustody, opt => opt.MapFrom(dest => dest.CustodyRiskDetail.RiskToPublic))
                .ForPath(src => src.RiskToKnownAdultInCustody, opt => opt.MapFrom(dest => dest.CustodyRiskDetail.RiskToKnownAdult))
                .ForPath(src => src.RiskToStaffInCustody, opt => opt.MapFrom(dest => dest.CustodyRiskDetail.RiskToStaff))
                .ForPath(src => src.RiskToSelfInCustody, opt => opt.MapFrom(dest => dest.CustodyRiskDetail.RiskToSelf))
                .ForPath(src => src.RiskToOtherPrisonersInCustody, opt => opt.MapFrom(dest => dest.CustodyRiskDetail.RiskToOtherPrisoners));
        }
    }

    public record class RiskDetail
    {
        [Description("Risk to Childen")]
        public RiskLevel? RiskToChildren { get; set; } 
        [Description("Risk to Public")]
        public RiskLevel? RiskToPublic { get; set; }
        [Description("Risk to Known Adult")]
        public RiskLevel? RiskToKnownAdult { get; set; }
        [Description("Risk to Staff")]
        public RiskLevel? RiskToStaff { get; set; }
        [Description("Risk to Self")]
        public RiskLevel? RiskToSelf { get; set; }
        [Description("Risk to Other Prisoners")]
        public RiskLevel? RiskToOtherPrisoners { get; set; }

        public class Validator : AbstractValidator<RiskDetail>
        {
            public Validator()
            {
                RuleFor(x => x.RiskToChildren)
                    .NotNull()
                    .WithMessage("This option is mandatory");

                RuleFor(x => x.RiskToPublic)
                    .NotNull()
                    .WithMessage("This option is mandatory");

                RuleFor(x => x.RiskToKnownAdult)
                    .NotNull()
                    .WithMessage("This option is mandatory");

                RuleFor(x => x.RiskToStaff)
                    .NotNull()
                    .WithMessage("This option is mandatory");

                RuleFor(x => x.RiskToOtherPrisoners)
                    .NotNull()
                    .WithMessage("This option is mandatory");

                RuleFor(x => x.RiskToSelf)
                    .NotNull()
                    .WithMessage("This option is mandatory");
            }
        }
    }

    public class Validator : AbstractValidator<RiskDto>
    { 
        public Validator()
        {
            RuleFor(x => x.ActivityRecommendations)
                .NotEmpty()
                .WithMessage("You must provide activity recommendations")
                .Matches(ValidationConstants.Notes)
                .WithMessage(string.Format(ValidationConstants.NotesMessage, "Activity Recommendations"));

            RuleFor(x => x.ActivityRecommendationsReceived)
                .NotEmpty()
                .WithMessage("You must provide the date activity recommendations were received")
                .LessThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage(ValidationConstants.DateMustBeInPast);

            RuleFor(x => x.ActivityRestrictions)
                .NotEmpty()
                .WithMessage("You must provide activity restrictions")
                .Matches(ValidationConstants.Notes)
                .WithMessage(string.Format(ValidationConstants.NotesMessage, "Activity Restrictions"));

            RuleFor(x => x.ActivityRestrictionsReceived)
                .NotEmpty()
                .WithMessage("You must provide the date activity restrictions were received")
                .LessThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage(ValidationConstants.DateMustBeInPast);

            RuleFor(x => x.AdditionalInformation)
                .NotEmpty()
                .WithMessage("You must provide additional information")
                .Matches(ValidationConstants.Notes)
                .WithMessage(string.Format(ValidationConstants.NotesMessage, "Additional Information"));

            RuleFor(x => x.LicenseConditions)
                .NotEmpty()
                .WithMessage("You must provide license conditions")
                .Matches(ValidationConstants.Notes)
                .WithMessage(string.Format(ValidationConstants.NotesMessage, "License Conditions"));

            RuleFor(x => x.LicenseEnd)
                .NotEmpty()
                .WithMessage("You must provide the license end date")
                .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage(ValidationConstants.DateMustBeInFuture);

            RuleFor(x => x.PSFRestrictions)
                .NotEmpty()
                .WithMessage("You must provide PSF restrictions")
                .Matches(ValidationConstants.Notes)
                .WithMessage(string.Format(ValidationConstants.NotesMessage, "PSF Restrictions"));

            RuleFor(x => x.PSFRestrictionsReceived)
                .NotEmpty()
                .WithMessage("You must provide the date PSF restrictions were received")
                .LessThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage(ValidationConstants.DateMustBeInPast);

            RuleFor(x => x.SpecificRisk)
                .NotEmpty()
                .WithMessage("You must provide specific risks")
                .Matches(ValidationConstants.Notes)
                .WithMessage(string.Format(ValidationConstants.NotesMessage, "Specific Risks"));

            RuleFor(x => x.IsRelevantToCommunity)
                .Equal(true)
                .When(x => x.IsRelevantToCustody is false)
                .WithMessage("You must pick one");

            RuleFor(x => x.IsRelevantToCustody)
                .Equal(true)
                .When(x => x.IsRelevantToCommunity is false)
                .WithMessage("You must pick one");

            When(x => x.IsRelevantToCommunity, () =>
            {
                RuleFor(x => x.CommunityRiskDetail)
                    .SetValidator(new RiskDetail.Validator());
            });

            When(x => x.IsRelevantToCustody, () =>
            {
                RuleFor(x => x.CustodyRiskDetail)
                    .SetValidator(new RiskDetail.Validator());
            });

            RuleFor(x => x.MappaCategory)
                .NotNull()
                .WithMessage("You must answer")
                .NotEqual(MappaCategory.NotApplicable)
                .When(x => x.MappaLevel != MappaLevel.NotApplicable)
                .WithMessage("This answer is incompatible with the selected level");

            RuleFor(x => x.MappaLevel)
                .NotNull()
                .WithMessage("You must answer")
                .NotEqual(MappaLevel.NotApplicable)
                .When(x => x.MappaCategory != MappaCategory.NotApplicable)
                .WithMessage("This answer is incompatible with the selected category");

            RuleFor(x => x.IsSubjectToSHPO)
                .NotNull()
                .WithMessage("You must answer");

            RuleFor(x => x.NSDCase)
                .NotNull()
                .WithMessage("You must answer");

            RuleFor(x => x.DeclarationSigned)
                .Equal(true)
                .WithMessage("You must confirm");

            When(x => x.DeclarationSigned, () =>
            {
                RuleFor(x => x.ReferrerName)
                    .NotEmpty()
                    .WithMessage("You must provide the referrers name")
                    .Matches(ValidationConstants.NameCompliantWithDMS)
                    .WithMessage(string.Format(ValidationConstants.NameCompliantWithDMSMessage, "Referrer Name"));

                RuleFor(x => x.ReferrerEmail)
                    .NotEmpty()
                    .WithMessage("You must provide the referrers email")
                    .EmailAddress()
                    .WithMessage("Must be a valid email address");

                RuleFor(x => x.ReferredOn)
                    .NotEmpty()
                    .WithMessage("You must provide the referral date")
                    .LessThanOrEqualTo(DateTime.UtcNow.Date)
                    .WithMessage(ValidationConstants.DateMustBeInPast);
            });

        }
    }

}
