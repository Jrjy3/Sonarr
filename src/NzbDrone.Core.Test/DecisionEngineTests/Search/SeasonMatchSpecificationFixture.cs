using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications.Search;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.DecisionEngineTests.Search
{
    [TestFixture]
    public class SeasonMatchSpecificationFixture : TestBase<SeasonMatchSpecification>
    {
        private Series _series;
        private RemoteEpisode _remoteEpisode;
        private SeasonSearchCriteria _searchCriteria;
        private ReleaseDecisionInformation _information;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew().With(s => s.Id = 1).Build();
            _remoteEpisode = new RemoteEpisode
            {
                Series = _series,
                ParsedEpisodeInfo = new ParsedEpisodeInfo
                {
                    SeasonNumber = 3,
                    IsMultiSeason = false,
                    SeasonNumbers = new[] { 3 }
                }
            };
            _searchCriteria = new SeasonSearchCriteria { Series = _series, SeasonNumber = 3 };
            _information = new ReleaseDecisionInformation(false, _searchCriteria);

            Mocker.SetConstant<ISceneMappingService>(Mocker.Resolve<SceneMappingService>());
        }

        [Test]
        public void should_return_true_when_season_matches()
        {
            Subject.IsSatisfiedBy(_remoteEpisode, _information).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_season_does_not_match()
        {
            _searchCriteria.SeasonNumber = 5;

            Subject.IsSatisfiedBy(_remoteEpisode, _information).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_no_search_criteria()
        {
            var information = new ReleaseDecisionInformation(false, null);

            Subject.IsSatisfiedBy(_remoteEpisode, information).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_for_multi_season_pack_containing_searched_season()
        {
            _remoteEpisode.ParsedEpisodeInfo = new ParsedEpisodeInfo
            {
                SeasonNumber = 1,
                IsMultiSeason = true,
                SeasonNumbers = new[] { 1, 2, 3, 4, 5 }
            };
            _searchCriteria.SeasonNumber = 3;

            Subject.IsSatisfiedBy(_remoteEpisode, _information).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_for_multi_season_pack_not_containing_searched_season()
        {
            _remoteEpisode.ParsedEpisodeInfo = new ParsedEpisodeInfo
            {
                SeasonNumber = 1,
                IsMultiSeason = true,
                SeasonNumbers = new[] { 1, 2, 3, 4, 5 }
            };
            _searchCriteria.SeasonNumber = 7;

            var result = Subject.IsSatisfiedBy(_remoteEpisode, _information);
            result.Accepted.Should().BeFalse();
            result.Message.Should().Contain("7");
        }

        [Test]
        public void should_return_true_for_anime_season_search_with_multi_season_pack()
        {
            var animeSearchCriteria = new AnimeSeasonSearchCriteria { Series = _series, SeasonNumber = 2 };
            var information = new ReleaseDecisionInformation(false, animeSearchCriteria);

            _remoteEpisode.ParsedEpisodeInfo = new ParsedEpisodeInfo
            {
                SeasonNumber = 1,
                IsMultiSeason = true,
                SeasonNumbers = new[] { 1, 2, 3 }
            };

            Subject.IsSatisfiedBy(_remoteEpisode, information).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_for_anime_season_search_with_multi_season_pack_not_containing_season()
        {
            var animeSearchCriteria = new AnimeSeasonSearchCriteria { Series = _series, SeasonNumber = 5 };
            var information = new ReleaseDecisionInformation(false, animeSearchCriteria);

            _remoteEpisode.ParsedEpisodeInfo = new ParsedEpisodeInfo
            {
                SeasonNumber = 1,
                IsMultiSeason = true,
                SeasonNumbers = new[] { 1, 2, 3 }
            };

            Subject.IsSatisfiedBy(_remoteEpisode, information).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_fall_back_to_season_number_when_multi_season_with_empty_season_numbers()
        {
            _remoteEpisode.ParsedEpisodeInfo = new ParsedEpisodeInfo
            {
                SeasonNumber = 3,
                IsMultiSeason = true,
                SeasonNumbers = System.Array.Empty<int>()
            };
            _searchCriteria.SeasonNumber = 3;

            // When IsMultiSeason is true but SeasonNumbers is empty, should fall back to single-season check
            Subject.IsSatisfiedBy(_remoteEpisode, _information).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_multi_season_with_empty_season_numbers_and_wrong_season()
        {
            _remoteEpisode.ParsedEpisodeInfo = new ParsedEpisodeInfo
            {
                SeasonNumber = 3,
                IsMultiSeason = true,
                SeasonNumbers = System.Array.Empty<int>()
            };
            _searchCriteria.SeasonNumber = 5;

            // When IsMultiSeason is true but SeasonNumbers is empty, should fall back to single-season check
            Subject.IsSatisfiedBy(_remoteEpisode, _information).Accepted.Should().BeFalse();
        }
    }
}
