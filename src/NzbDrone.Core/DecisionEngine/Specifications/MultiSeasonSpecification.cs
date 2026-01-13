using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class MultiSeasonSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public MultiSeasonSpecification(IConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual DownloadSpecDecision IsSatisfiedBy(RemoteEpisode subject, ReleaseDecisionInformation information)
        {
            if (!subject.ParsedEpisodeInfo.IsMultiSeason)
            {
                return DownloadSpecDecision.Accept();
            }

            var multiSeasonSetting = _configService.MultiSeasonPack;

            if (multiSeasonSetting == MultiSeasonPackType.Disabled)
            {
                _logger.Debug("Multi-season release {0} rejected. Multi-season packs are disabled", subject.Release.Title);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.MultiSeason, "Multi-season packs are disabled");
            }

            var seasonNumbers = subject.ParsedEpisodeInfo.SeasonNumbers;

            if (seasonNumbers == null || seasonNumbers.Length == 0)
            {
                _logger.Debug("Multi-season release {0} rejected. No season numbers found", subject.Release.Title);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.MultiSeason, "No season numbers found in multi-season pack");
            }

            var series = subject.Series;

            if (series == null)
            {
                return DownloadSpecDecision.Accept();
            }

            // Get wanted (monitored) seasons from the pack
            var wantedSeasons = series.Seasons
                .Where(s => s.Monitored && seasonNumbers.Contains(s.SeasonNumber))
                .Select(s => s.SeasonNumber)
                .ToList();

            if (!wantedSeasons.Any())
            {
                _logger.Debug(
                    "Multi-season release {0} rejected. No monitored seasons in pack (seasons: {1})",
                    subject.Release.Title,
                    string.Join(", ", seasonNumbers));

                return DownloadSpecDecision.Reject(DownloadRejectionReason.MultiSeason, "No monitored seasons in multi-season pack");
            }

            // Check threshold - allows downloading packs with some unwanted seasons
            var wantedPercentage = (double)wantedSeasons.Count / seasonNumbers.Length * 100;
            var threshold = _configService.MultiSeasonPackThreshold;

            if (wantedPercentage < threshold)
            {
                _logger.Debug(
                    "Multi-season release {0} rejected. Only {1:F0}% of seasons wanted (threshold: {2}%)",
                    subject.Release.Title,
                    wantedPercentage,
                    threshold);

                return DownloadSpecDecision.Reject(
                    DownloadRejectionReason.MultiSeason,
                    "Only {0:F0}% of seasons in pack are wanted (threshold: {1}%)",
                    wantedPercentage,
                    threshold);
            }

            _logger.Debug(
                "Multi-season release {0} accepted. Wanted seasons: {1} ({2:F0}% of pack)",
                subject.Release.Title,
                string.Join(", ", wantedSeasons),
                wantedPercentage);

            return DownloadSpecDecision.Accept();
        }
    }
}
