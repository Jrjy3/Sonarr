using System.Linq;
using NLog;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class SeasonMatchSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;
        private readonly ISceneMappingService _sceneMappingService;

        public SeasonMatchSpecification(ISceneMappingService sceneMappingService, Logger logger)
        {
            _logger = logger;
            _sceneMappingService = sceneMappingService;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteEpisode remoteEpisode, ReleaseDecisionInformation information)
        {
            if (information.SearchCriteria == null)
            {
                return DownloadSpecDecision.Accept();
            }

            // Handle both standard and anime season searches
            int? searchedSeason = null;

            if (information.SearchCriteria is SeasonSearchCriteria seasonSearchSpec)
            {
                searchedSeason = seasonSearchSpec.SeasonNumber;
            }
            else if (information.SearchCriteria is AnimeSeasonSearchCriteria animeSeasonSearchSpec)
            {
                searchedSeason = animeSeasonSearchSpec.SeasonNumber;
            }

            if (!searchedSeason.HasValue)
            {
                return DownloadSpecDecision.Accept();
            }

            var parsedInfo = remoteEpisode.ParsedEpisodeInfo;

            // For multi-season packs, check if the searched season is within the pack's season range
            if (parsedInfo.IsMultiSeason && parsedInfo.SeasonNumbers != null && parsedInfo.SeasonNumbers.Length > 0)
            {
                if (!parsedInfo.SeasonNumbers.Contains(searchedSeason.Value))
                {
                    _logger.Debug("Multi-season pack does not contain searched season {0}, skipping.", searchedSeason.Value);
                    return DownloadSpecDecision.Reject(DownloadRejectionReason.WrongSeason, "Multi-season pack does not contain season {0}", searchedSeason.Value);
                }

                return DownloadSpecDecision.Accept();
            }

            // Standard single-season check
            if (searchedSeason.Value != parsedInfo.SeasonNumber)
            {
                _logger.Debug("Season number does not match searched season number, skipping.");
                return DownloadSpecDecision.Reject(DownloadRejectionReason.WrongSeason, "Wrong season");
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
