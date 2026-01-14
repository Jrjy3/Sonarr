import React, {
  createContext,
  PropsWithChildren,
  useContext,
  useMemo,
} from 'react';
import useApiQuery from 'Helpers/Hooks/useApiQuery';
import Queue from 'typings/Queue';

interface EpisodeDetails {
  episodeIds: number[];
}

interface SeriesDetails {
  seriesId: number;
}

interface AllDetails {
  all: boolean;
}

type QueueDetailsFilter = AllDetails | EpisodeDetails | SeriesDetails;

const QueueDetailsContext = createContext<Queue[] | undefined>(undefined);

export default function QueueDetailsProvider({
  children,
  ...filter
}: PropsWithChildren<QueueDetailsFilter>) {
  const { data } = useApiQuery<Queue[]>({
    path: '/queue/details',
    queryParams: { ...filter, includeSubresources: ['Episodes'] },
    queryOptions: {
      enabled: Object.keys(filter).length > 0,
    },
  });

  return (
    <QueueDetailsContext.Provider value={data}>
      {children}
    </QueueDetailsContext.Provider>
  );
}

export function useQueueItemForEpisode(episodeId: number) {
  const queue = useContext(QueueDetailsContext);

  return useMemo(() => {
    return queue?.find((item) => item.episodeIds.includes(episodeId));
  }, [episodeId, queue]);
}

export function useIsDownloadingEpisodes(episodeIds: number[]) {
  const queue = useContext(QueueDetailsContext);

  return useMemo(() => {
    if (!queue) {
      return false;
    }

    return queue.some((item) =>
      item.episodeIds?.some((e) => episodeIds.includes(e))
    );
  }, [episodeIds, queue]);
}

export interface SeriesQueueDetails {
  count: number;
  episodesWithFiles: number;
}

interface AccumulatorWithTracking extends SeriesQueueDetails {
  seenEpisodeIds: Set<number>;
}

export function useQueueDetailsForSeries(
  seriesId: number,
  seasonNumber?: number
) {
  const queue = useContext(QueueDetailsContext);

  return useMemo<SeriesQueueDetails>(() => {
    if (!queue) {
      return { count: 0, episodesWithFiles: 0 };
    }

    const result = queue.reduce<AccumulatorWithTracking>(
      (acc, item) => {
        if (
          item.trackedDownloadState === 'imported' ||
          item.seriesId !== seriesId
        ) {
          return acc;
        }

        // For multi-season packs, check if the season is in the seasonNumbers array
        // Fall back to single seasonNumber for backward compatibility with legacy queue items
        if (seasonNumber != null) {
          const hasSeasonNumbers = item.seasonNumbers?.length > 0;
          const matchesSeason = hasSeasonNumbers
            ? item.seasonNumbers.includes(seasonNumber)
            : item.seasonNumber === seasonNumber;

          if (!matchesSeason) {
            return acc;
          }
        }

        // Count actual episodes, not queue items, and deduplicate by episode ID
        if (seasonNumber != null && item.episodes?.length) {
          // Filter to only count episodes for this specific season
          const seasonEpisodes = item.episodes.filter(
            (e) => e.seasonNumber === seasonNumber && !acc.seenEpisodeIds.has(e.id)
          );
          seasonEpisodes.forEach((e) => acc.seenEpisodeIds.add(e.id));
          acc.count += seasonEpisodes.length;
          acc.episodesWithFiles += seasonEpisodes.filter(
            (e) => e.hasFile
          ).length;
        } else if (item.episodeIds?.length) {
          // For series-level counts, count all episodes in the queue item (deduplicated)
          const newEpisodeIds = item.episodeIds.filter(
            (id) => !acc.seenEpisodeIds.has(id)
          );
          newEpisodeIds.forEach((id) => acc.seenEpisodeIds.add(id));
          acc.count += newEpisodeIds.length;
          // For episodesWithFiles, we need to count from episodes array if available
          if (item.episodes?.length) {
            acc.episodesWithFiles += item.episodes.filter(
              (e) => newEpisodeIds.includes(e.id) && e.hasFile
            ).length;
          } else {
            // Fallback: proportionally estimate episodesWithFiles when the episodes array
            // is not available (e.g., when includeSubresources doesn't include Episodes).
            // This may be inaccurate if episode file distribution is uneven across the pack.
            const ratio = newEpisodeIds.length / item.episodeIds.length;
            acc.episodesWithFiles += Math.round((item.episodesWithFilesCount ?? 0) * ratio);
          }
        } else {
          // Fallback for single episode
          const episodeId = item.episodeId;
          if (episodeId && !acc.seenEpisodeIds.has(episodeId)) {
            acc.seenEpisodeIds.add(episodeId);
            acc.count++;
            if (item.episodeHasFile) {
              acc.episodesWithFiles++;
            }
          }
        }

        return acc;
      },
      {
        count: 0,
        episodesWithFiles: 0,
        seenEpisodeIds: new Set<number>(),
      }
    );

    return { count: result.count, episodesWithFiles: result.episodesWithFiles };
  }, [seriesId, seasonNumber, queue]);
}

export const useQueueDetails = () => {
  return useContext(QueueDetailsContext) ?? [];
};
