namespace Console
{
    using System;

    public class MusicBrainz
    {
        private static readonly Uri API_ROOT = new Uri("https://musicbrainz.org/ws/2");

        private static Uri GetArtistSearchRequestUri(string query) => new Uri($"{API_ROOT}/artist/?query={Uri.EscapeDataString(query)}&fmt=json");

        private static Uri GetReleaseGroupRequestUri(Guid artistMbid, int offset, int limit) => new Uri($"{API_ROOT}/release-group?artist={artistMbid}&type=album|ep&offset={offset}&limit={limit}&fmt=json");

        private static Uri GetReleaseRequestUri(Guid releaseGroupMbid, int offset, int limit) => new Uri($"{API_ROOT}/release?release-group={releaseGroupMbid}&offset={offset}&limit={limit}&inc=media+recordings&fmt=json");
    }
}
