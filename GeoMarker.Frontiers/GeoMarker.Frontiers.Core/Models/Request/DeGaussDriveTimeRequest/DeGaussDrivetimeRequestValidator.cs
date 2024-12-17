using FluentValidation;
using GeoMarker.Frontiers.Core.Resources;
using Microsoft.Extensions.Options;

namespace GeoMarker.Frontiers.Core.Models.Request.Validation
{
    public class DeGaussDrivetimeRequestValidator : AbstractValidator<DeGaussDrivetimeRequest>
    {
        public readonly static Dictionary<string, string> SITES = new Dictionary<string, string>()
        {
            { "Children's Mercy Hospital (mercy)", "mercy" },
            { "Children’s Mercy Sports Medicine Center (cmsmc)", "cmsmc" },
            { "Children's Mercy Hospital Kansas (cmhk)", "cmhk" },
            { "Children's Mercy College Boulevard (cmcb)", "cmcb" },
            { "Children's Mercy Urgent Care: Blue Valley (cmucbv)", "cmucbv" },
            { "Children’s Mercy Olathe (cmo)", "cmo" },
            { "Children's Mercy Broadway (cmb)", "cmb" },
            { "Children's Mercy Urgent Care: Northland (cmucn)", "cmucn" },
            { "Children's Mercy Urgent Care: East (cmuce)", "cmuce" },
            { "Children’s Mercy Topeka (cmt)", "cmt" },
            { "Children's Mercy St. Joseph (cmhstj)", "cmhstj" },
            { "Children's Mercy Junction City (cmjc)", "cmjc" },
            { "Children's Mercy Joplin (cmj)", "cmj" },
            { "Children's Mercy Wichita (cmwt)", "cmwt" },
            { "Children's Mercy Great Bend (cmgb)", "cmgb" },
            { "University of Kansas Main Campus (kumc)", "kumc" },
            { "University of Kansas Cancer Center - Indian Creek (indck)", "indck" },
            { "University of Kansas Cancer Center - Olathe (ccocg)", "ccocg" },
            { "University of Kansas Cancer Center - Lee's Summit (leesm)", "leesm" },
            { "University of Kansas Cancer Center - Overland Park (ovpk)", "ovpk" },
            { "University of Kansas Cancer Center - St. Francis (stfr)", "stfr" },
            { "University of Kansas Cancer Center - Westwood (ww)", "ww" },
            { "University of Kansas Cancer Center - Fairway (fairw)", "fairw" },
            { "University of Kansas Cancer Center - USO Cancer Center (uso)", "uso" },
            { "University of Kansas Cancer Center - Sarcoma Center - Indian Creek (scic)", "scic" },
            { "Children's Hospital of Philadelphia (chop)", "chop" },
            { "Cincinnati Children's Hospital Medical Center (cchmc)", "cchmc" },
            { "Riley Hospital for Children, Indiana University (riley)", "riley" },
            { "Seattle Children's Hospital (seattle)", "seattle" },
            { "Emory University (emory)", "emory" },
            { "Johns Hopkins University (jhu)", "jhu" },
            { "Cleveland Clinic (cc)", "cc" },
            { "Levine Children's (levine)", "levine" },
            { "St. Louis Children's Hospital (stl)", "stl" },
            { "Oregon Health and Science University (ohsu)", "ohsu" },
            { "University of Michigan Health System (umich)", "umich" },
            { "Children's Hospital of Alabama (al)", "al" },
            { "Nationwide Children's Hospital (nat)", "nat" },
            { "University of California, Los Angeles (ucla)", "ucla" },
            { "Boston Children's Hospital (bch)", "bch" },
            { "Medical College of Wisconsin (mcw)", "mcw" },
            { "St. Jude's Children's Hospital (stj)", "stj" },
            { "Martha Eliot Health Center (mehc)", "mehc" },
            { "Ann & Lurie Children's / Northwestern (nwu)", "nwu" },
            { "Lurie Children's Center in Northbrook (lccn)", "lccn" },
            { "Lurie Children's Center in Lincoln Park (lcclp)", "lcclp" },
            { "Lurie Children's Center in Uptown (lccu)", "lccu" },
            { "Dr. Lio's and Dr. Aggarwal's Clinics (lac)", "lac" },
            { "Recruited from Eczema Expo 2018 (expo)", "expo" },
            { "University of California San Francisco Benioff Children's Hospital (ucsf)", "ucsf" },
            { "Nicklaus Children's Hospital (nicklaus)", "nicklaus" },
            { "Medical University of South Carolina Children's Hospital (musc)", "musc" },
            { "Children's National Medical Center (cnmc)", "cnmc" },
            { "Children's Hospital of Pittsburgh of UPMC (upmc)", "upmc" },
            { "Methodist LeBonheur Children's Hospital (methodist)", "methodist" },
            { "Texas Children's Hospital (texas)", "texas" },
            { "Arkansas Children's Hospital (arkansas)", "arkansas" },
            { "Primary Children's Medical Center (primary)", "primary" },
            { "Children's Healthcare of Atlanta (atlanta)", "atlanta" },
            { "Children's Medical Center of Dallas (dallas)", "dallas" },
            { "Lucile Packard Children's Hospital Stanford (packard)", "packard" },
            { "Toronto Hospital for Sick Children (toronto)", "toronto" },
            { "Cook Children's Medical Center (cook)", "cook" },
            { "Children's Hospital & Medical Center - Omaha (omaha)", "omaha" },
            { "Children's Hospital Colorado (colorado)", "colorado" },
            { "Arnold Palmer Hospital for Children (palmer)", "palmer" },
            { "Children's Hospital & Clinics of Minnesota (minn)", "minn" },
            { "University of Virginia Hospital (uva)", "uva" },
            { "Joe Dimaggio Children's Hospital (dimaggio)", "dimaggio" },
            { "Cohen Children's Medical Center of New York at Northwell Health (cohen)", "cohen" },
            { "Dell Children's Medical Center of Central Texas (dell)", "dell" },
            { "A.I. duPont Hospital for Children (dupont)", "dupont" },
            { "Rainbow Babies and Children's Hospital (rainbow)", "rainbow" },
            { "UNC Hospitals Children's Specialty Clinic (unc)", "unc" },
            { "Barbara Bush Children's Hospital at Maine Medical (maine)", "maine" }
        };

        protected readonly static string _siteInvalid = "'site' must be one of: " + SITES.Aggregate("", (acc, next) => acc += " " + next);

        public DeGaussDrivetimeRequestValidator(IOptions<FileMetadata> fileMetadata)
        {
            Include(new DeGaussRequestValidator(fileMetadata));

            RuleSet("Drivetime", () =>
            {
                RuleFor(x => x.Site).NotNull().WithMessage(CoreMessages.ValidatorController_SiteNullMessage);
                RuleFor(x => x.Site).NotEmpty().WithMessage(CoreMessages.ValidatorController_SiteEmptyMessage);
                RuleFor(x => x.Site).Must(site => SITES.Values.ToList().Contains(site!))
                                .WithMessage(string.Format(CoreMessages.ValidatorController_SiteInvalidMessage, SITES.Values.ToList().Aggregate("", (acc, next) => acc += " " + next)));
            });
        }
    }
}
