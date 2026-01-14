using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class MultiSeasonSpecificationFixture : CoreTest<MultiSeasonSpecification>
    {
        private RemoteEpisode _remoteEpisode;
        private Series _series;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew()
                .With(s => s.Id = 1234)
                .With(s => s.Seasons = new List<Season>
                {
                    new Season { SeasonNumber = 1, Monitored = true },
                    new Season { SeasonNumber = 2, Monitored = true },
                    new Season { SeasonNumber = 3, Monitored = true },
                    new Season { SeasonNumber = 4, Monitored = false },
                    new Season { SeasonNumber = 5, Monitored = false }
                })
                .Build();

            _remoteEpisode = new RemoteEpisode
            {
                ParsedEpisodeInfo = new ParsedEpisodeInfo
                {
                    FullSeason = true,
                    IsMultiSeason = true,
                    SeasonNumbers = new[] { 1, 2, 3, 4, 5 }
                },
                Episodes = Builder<Episode>.CreateListOfSize(3)
                                           .All()
                                           .With(s => s.SeriesId = _series.Id)
                                           .BuildList(),
                Series = _series,
                Release = new ReleaseInfo
                {
                    Title = "Series.Title.S01-05.720p.BluRay.X264-RlsGrp"
                }
            };

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MultiSeasonPack)
                .Returns(MultiSeasonPackType.Disabled);

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MultiSeasonPackThreshold)
                .Returns(50.0);
        }

        [Test]
        public void should_return_true_if_is_not_a_multi_season_release()
        {
            _remoteEpisode.ParsedEpisodeInfo.IsMultiSeason = false;

            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_multi_season_is_disabled()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MultiSeasonPack)
                .Returns(MultiSeasonPackType.Disabled);

            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_multi_season_is_enabled_and_threshold_met()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MultiSeasonPack)
                .Returns(MultiSeasonPackType.NoPreference);

            // 3 out of 5 seasons are monitored = 60%, threshold is 50%
            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_multi_season_is_enabled_but_threshold_not_met()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MultiSeasonPack)
                .Returns(MultiSeasonPackType.NoPreference);

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MultiSeasonPackThreshold)
                .Returns(80.0);

            // 3 out of 5 seasons are monitored = 60%, threshold is 80%
            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_no_monitored_seasons_in_pack()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MultiSeasonPack)
                .Returns(MultiSeasonPackType.NoPreference);

            // Pack only contains seasons 4 and 5, which are not monitored
            _remoteEpisode.ParsedEpisodeInfo.SeasonNumbers = new[] { 4, 5 };

            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_all_seasons_in_pack_are_monitored()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MultiSeasonPack)
                .Returns(MultiSeasonPackType.PreferMultiSeason);

            // Pack only contains seasons 1, 2, 3, which are all monitored
            _remoteEpisode.ParsedEpisodeInfo.SeasonNumbers = new[] { 1, 2, 3 };

            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_season_numbers_array_is_empty()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MultiSeasonPack)
                .Returns(MultiSeasonPackType.NoPreference);

            _remoteEpisode.ParsedEpisodeInfo.SeasonNumbers = System.Array.Empty<int>();

            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_threshold_exactly_met()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MultiSeasonPack)
                .Returns(MultiSeasonPackType.NoPreference);

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MultiSeasonPackThreshold)
                .Returns(60.0);

            // 3 out of 5 seasons are monitored = 60%, threshold is exactly 60%
            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }
    }
}
