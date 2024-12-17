using FluentValidation;
using GeoMarker.Frontiers.Core.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GeoMarker.Frontiers.Core.Models.Request.Validation
{
    public class DeGaussRequestValidator : AbstractValidator<DeGaussRequest>
    {
        public readonly static int MAX_LINES = 300;
        private readonly long _expectedFileSizeInBytes = 25000000;
        private readonly static List<string> CONTENT_TYPES = new List<string> { "text/csv" };
        private readonly FileMetadata _fileMetadata;
        public DeGaussRequestValidator(IOptions<FileMetadata> fileMetadata)
        {
            _fileMetadata = fileMetadata.Value;

            long maxFileSizeInBytes = _fileMetadata.MaxFileSizeInBytes;

            if (maxFileSizeInBytes > 0)
            {
                _expectedFileSizeInBytes = maxFileSizeInBytes;
            }

            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleSet("Base", () =>
            {
                RuleFor(x => x.File).NotNull().WithMessage(CoreMessages.ValidatorController_FileNullMessage);

                When(x => x.File != null, () =>
                {
                    RuleFor(x => x.File!.ContentType).NotNull().NotEmpty();
                    RuleFor(x => x.File!.ContentType).Must(x => CONTENT_TYPES.Contains(x)).WithMessage(string.Format(CoreMessages.ValidatorController_FileIncorrectContentTypeMessage, CONTENT_TYPES.Aggregate((concat, str) => $"{concat}, {str}")));
                    RuleFor(x => x.File).Must(x => RecordCountAboveMin(x!)).WithMessage(CoreMessages.ValidatorController_FileEmptyMessage);                  
                    RuleFor(x => x.File).Must(x => RecordHeaderHasNoDuplicates(x!)).WithMessage(CoreMessages.ValidatorController_FileDuplicateHeadersMessage);
                });
            });

            RuleSet("BelowMaxRows", () =>
            {
                RuleFor(x => x.File).Must(x => RecordCountBelowMax(x!)).WithMessage(string.Format(CoreMessages.ValidatorController_FileExceedsRowCountMessage, MAX_LINES));
            });           
            
            RuleSet("Geocoded", () =>
            {
                RuleFor(x => x.File).Must(x => RecordSchemaGeocoded(x!)).WithMessage(CoreMessages.ValidatorController_FileIncorrectSchemaGeocodedMessage);
            });

            RuleSet("Geocode", () =>
            {              
                RuleFor(x => x.File).Must(x => RecordSchemaGetGeocodeHasColumns(x!)).WithMessage(CoreMessages.ValidatorController_FileNeedsGeocodeColumns);
                RuleFor(x => x.File).Must(x => RecordSchemaGetGeocodeDoesNotHaveColumns(x!)).WithMessage(CoreMessages.ValidatorController_FileCannotHaveGeocodedColumns);
            });

            var fileSizeInMb = (_expectedFileSizeInBytes / 1024f) / 1024f;
            RuleSet("MaxFileSize", () =>
            {
                RuleFor(x => x.File).Must(x => MaxFileSize(x!)).WithMessage(string.Format(CoreMessages.ValidatorController_FileExceedsSize, Math.Ceiling(fileSizeInMb).ToString() + " MB"));
            });
            RuleSet("ValidateFileName", () =>
            {
                RuleFor(x => x.File).Must(x => ValidateFileName(x!)).WithMessage(CoreMessages.ValidatorController_ValidateFileName);
            });

            RuleSet("ValidateFileNameGeocode", () =>
            {
                RuleFor(x => x.File).Must(x => !x!.FileName.Contains("geocoder", StringComparison.OrdinalIgnoreCase)).WithMessage(string.Format(CoreMessages.ValidatorController_ReservedFileName, "geocoder", "Geocode"));
            });
            RuleSet("ValidateFileNameDriveTime", () =>
            {
                RuleFor(x => x.File).Must(x => !x!.FileName.Contains("drivetime", StringComparison.OrdinalIgnoreCase)).WithMessage(string.Format(CoreMessages.ValidatorController_ReservedFileName, "drivetime", "Drive Time"));
            });
            RuleSet("ValidateFileNameDeprivationIndex", () =>
            {
                RuleFor(x => x.File).Must(x => !x!.FileName.Contains("dep_index", StringComparison.OrdinalIgnoreCase)).WithMessage(string.Format(CoreMessages.ValidatorController_ReservedFileName, "dep_index", "Deprivation Index"));
            });
            RuleSet("ValidateFileNameCensusBlockGroup", () =>
            {
                RuleFor(x => x.File).Must(x => !x!.FileName.Contains("census_block_group", StringComparison.OrdinalIgnoreCase)).WithMessage(string.Format(CoreMessages.ValidatorController_ReservedFileName, "census_block_group", "Census Block Group"));
            });
        }

        protected bool RecordCountBelowMax(IFormFile file)
        {
            int counter = 0;

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (reader.ReadLine() != null)
                {
                    counter++;
                }
            }

            return counter <= MAX_LINES + 1;
        }

        protected bool RecordCountAboveMin(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                return reader.ReadLine() != null && reader.ReadLine() != null;
            }
        }
        protected bool RecordHeaderHasNoDuplicates(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var header = reader.ReadLine();
                if (header == null) return false;
                var headerArray = header.Split(',');

                return headerArray.Distinct().Count() == headerArray.Count();
            }
        }

        protected bool RecordSchemaGeocoded(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var header = reader.ReadLine();
                if (header == null) return false;
                string[] headerArray = header.Split(',');

                return headerArray.Contains("lat", StringComparer.OrdinalIgnoreCase) &&
                       headerArray.Contains("lon", StringComparer.OrdinalIgnoreCase);
            }
        }

        protected bool RecordSchemaGetGeocodeHasColumns(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var header = reader.ReadLine();
                if (header == null) return false;
                string[] headerArray = header.Split(',');

                return headerArray.Contains("ID", StringComparer.OrdinalIgnoreCase) &&
                       headerArray.Contains("address");
            }
        }

        protected bool RecordSchemaGetGeocodeDoesNotHaveColumns(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var header = reader.ReadLine();
                if (header == null) return false;
                string[] headerArray = header.Split(',');

                return !headerArray.Contains("lat", StringComparer.OrdinalIgnoreCase) &&
                       !headerArray.Contains("lon", StringComparer.OrdinalIgnoreCase) &&
                       !headerArray.Contains("score", StringComparer.OrdinalIgnoreCase) &&
                       !headerArray.Contains("precision", StringComparer.OrdinalIgnoreCase);
            }
        }

        protected bool MaxFileSize(IFormFile file)
        {
            return file?.Length < _expectedFileSizeInBytes;
        }

        protected bool ValidateFileName(IFormFile file)
        {
            var fileName = file.FileName;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            Regex regularExpression = new Regex("^[a-zA-Z0-9-_]*$");          
            bool ValidFileName = regularExpression.IsMatch(fileNameWithoutExtension);
            return ValidFileName;
        }
    }
}
