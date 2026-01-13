using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalEpisode
    {
        public LocalEpisode()
        {
            Episodes = new List<Episode>();
            Languages = new List<Language>();
            CustomFormats = new List<CustomFormat>();
        }

        public string Path { get; set; }
        public long Size { get; set; }
        public ParsedEpisodeInfo FileEpisodeInfo { get; set; }
        public ParsedEpisodeInfo DownloadClientEpisodeInfo { get; set; }
        public DownloadClientItem DownloadItem { get; set; }
        public ParsedEpisodeInfo FolderEpisodeInfo { get; set; }
        public Series Series { get; set; }
        public List<Episode> Episodes { get; set; }
        public List<DeletedEpisodeFile> OldFiles { get; set; }
        public QualityModel Quality { get; set; }
        public List<Language> Languages { get; set; }
        public IndexerFlags IndexerFlags { get; set; }
        public ReleaseType ReleaseType { get; set; }
        public MediaInfoModel MediaInfo { get; set; }
        public bool ExistingFile { get; set; }
        public bool SceneSource { get; set; }
        public string ReleaseGroup { get; set; }
        public string ReleaseHash { get; set; }
        public string SceneName { get; set; }
        public bool OtherVideoFiles { get; set; }
        public List<CustomFormat> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public GrabbedReleaseInfo Release { get; set; }
        public bool ScriptImported { get; set; }
        public string FileNameBeforeRename { get; set; }
        public bool ShouldImportExtras { get; set; }
        public List<string> PossibleExtraFiles { get; set; }
        public SubtitleTitleInfo SubtitleInfo { get; set; }

        public int SeasonNumber
        {
            get
            {
                var seasons = Episodes.Select(c => c.SeasonNumber).Distinct().ToList();

                if (seasons.Empty())
                {
                    throw new InvalidSeasonException("Expected at least one season, but found none");
                }

                // For multi-season files, return the first (lowest) season number for backward compatibility
                if (seasons.Count > 1)
                {
                    return seasons.Min();
                }

                return seasons.Single();
            }
        }

        public int[] SeasonNumbers
        {
            get
            {
                return Episodes.Select(c => c.SeasonNumber).Distinct().OrderBy(s => s).ToArray();
            }
        }

        public bool IsMultiSeason => SeasonNumbers.Length > 1;

        public bool IsSpecial => SeasonNumber == 0;

        public override string ToString()
        {
            return Path;
        }
    }
}
