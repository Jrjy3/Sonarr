using System.Linq;
using NLog;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class SingleEpisodeSearchMatchSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;
        private readonly ISceneMappingService _sceneMappingService;

        public SingleEpisodeSearchMatchSpecification(ISceneMappingService sceneMappingService, Logger logger)
        {
            _logger = logger;
            _sceneMappingService = sceneMappingService;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteEpisode remoteEpisode, ReleaseDecisionInformation information)
        {
            var searchCriteria = information.SearchCriteria;

            if (searchCriteria == null)
            {
                return DownloadSpecDecision.Accept();
            }

            if (searchCriteria is SingleEpisodeSearchCriteria singleEpisodeSpec)
            {
                return IsSatisfiedBy(remoteEpisode, singleEpisodeSpec);
            }

            if (searchCriteria is AnimeEpisodeSearchCriteria animeEpisodeSpec)
            {
                return IsSatisfiedBy(remoteEpisode, animeEpisodeSpec);
            }

            return DownloadSpecDecision.Accept();
        }

        private DownloadSpecDecision IsSatisfiedBy(RemoteEpisode remoteEpisode, SingleEpisodeSearchCriteria singleEpisodeSpec)
        {
            var parsedInfo = remoteEpisode.ParsedEpisodeInfo;

            // For multi-season packs, check if searched season is within the pack
            if (parsedInfo.IsMultiSeason && parsedInfo.SeasonNumbers != null && parsedInfo.SeasonNumbers.Length > 0)
            {
                if (!parsedInfo.SeasonNumbers.Contains(singleEpisodeSpec.SeasonNumber))
                {
                    _logger.Debug("Multi-season pack does not contain searched season {0}, skipping.", singleEpisodeSpec.SeasonNumber);
                    return DownloadSpecDecision.Reject(DownloadRejectionReason.WrongSeason, "Multi-season pack does not contain season {0}", singleEpisodeSpec.SeasonNumber);
                }

                // Multi-season full season packs are rejected during single episode search
                // (handled by FullSeason check below)
            }
            else if (singleEpisodeSpec.SeasonNumber != parsedInfo.SeasonNumber)
            {
                _logger.Debug("Season number does not match searched season number, skipping.");
                return DownloadSpecDecision.Reject(DownloadRejectionReason.WrongSeason, "Wrong season");
            }

            if (!parsedInfo.EpisodeNumbers.Any())
            {
                _logger.Debug("Full season result during single episode search, skipping.");
                return DownloadSpecDecision.Reject(DownloadRejectionReason.FullSeason, "Full season pack");
            }

            if (!parsedInfo.EpisodeNumbers.Contains(singleEpisodeSpec.EpisodeNumber))
            {
                _logger.Debug("Episode number does not match searched episode number, skipping.");
                return DownloadSpecDecision.Reject(DownloadRejectionReason.WrongEpisode, "Wrong episode");
            }

            return DownloadSpecDecision.Accept();
        }

        private DownloadSpecDecision IsSatisfiedBy(RemoteEpisode remoteEpisode, AnimeEpisodeSearchCriteria animeEpisodeSpec)
        {
            if (remoteEpisode.ParsedEpisodeInfo.FullSeason && !animeEpisodeSpec.IsSeasonSearch)
            {
                _logger.Debug("Full season result during single episode search, skipping.");
                return DownloadSpecDecision.Reject(DownloadRejectionReason.FullSeason, "Full season pack");
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
