<p align="center">
  <img src=".github/assets/banner.png" alt="CrowdMix — crowd-powered instant mix" width="100%" />
</p>

# CrowdMix

> A better Jellyfin Instant Mix, powered by the **listening crowd** — not just audio.

CrowdMix replaces Jellyfin's native Instant Mix with recommendations sourced from a real
listening population via the [Last.fm API](https://www.last.fm/api). It's a deliberately
different bet from audio-embedding plugins like
[BetterMix](https://github.com/StergiosBinopoulos/jellyfin-plugin-bettermix)/Deej-AI:

| | CrowdMix (this) | Audio-embedding (Deej-AI/BetterMix) |
|---|---|---|
| Signal | **Collaborative filtering** — what real people play together | Audio spectrogram similarity |
| Closeness to Spotify's "magic" | High (same core signal) | Approximation |
| Setup cost | API key, no scanning | Hours of GPU/CPU scanning |
| Long-tail / obscure tracks | Weaker (needs a catalog match) | Stronger |
| Privacy | Sends artist+title metadata only | Fully local |

The insight: the thing that makes commercial recommendations feel hand-picked is the
**wisdom of the crowd** ("people who played X also played Y"), which audio models can only
imitate. Last.fm exposes that signal directly. CrowdMix uses it for candidate generation,
then **re-ranks against your own library and play history** — mirroring the real
"retrieve then learned-rerank" architecture.

## How it works

1. **Match** — your seed track is normalized (case/accents/`feat.`/remaster noise stripped)
   and matched to the Last.fm catalog by artist + title.
2. **Generate candidates** — `track.getSimilar` returns the crowd's similar tracks; when
   sparse, CrowdMix widens via `artist.getSimilar` → `artist.getTopTracks`.
3. **Intersect** — candidates are matched back against tracks **you actually own**.
4. **Re-rank** — a hybrid score fuses crowd similarity + genre/tag overlap + your play
   affinity + favorites, with a configurable per-artist cap so the seed artist can't
   dominate the mix.
5. **Fallback** — if a seed has no crowd match (obscure/long-tail), CrowdMix transparently
   returns Jellyfin's native mix, so you never get an empty playlist.

It also builds a per-user **CrowdMix Daily** playlist from your most-played tracks.

## Install

1. In Jellyfin: **Dashboard → Plugins → Repositories → Add**, URL:
   `https://raw.githubusercontent.com/johnpc/jellyfin-plugin-crowdmix/main/manifest.json`
2. **Catalog → CrowdMix → Install**, then restart Jellyfin.
3. **Dashboard → Plugins → CrowdMix**, paste your free
   [Last.fm API key](https://www.last.fm/api/account/create), and Save.

## Configuration

| Setting | Default | Purpose |
|---|---|---|
| Last.fm API Key | — | Required; reads the crowd signal |
| Override native Instant Mix | on | Turn CrowdMix on/off without uninstalling |
| Fall back to native mix | on | Use native mix when no crowd match |
| Instant Mix size | 50 | Target tracks per mix |
| Crowd similarity weight | 1.0 | Influence of Last.fm match score |
| Tag/genre overlap weight | 0.4 | Reward candidates sharing the seed's genres |
| Play-history affinity weight | 0.3 | Reward tracks you play often |
| Favorite bonus weight | 0.25 | Reward favorited tracks |
| Max tracks per artist | 2 | Keep the mix varied |
| Daily mix | off | Build a per-user daily playlist |

## Development

```bash
dotnet build --configuration Release          # build the plugin
dotnet test  Jellyfin.Plugin.CrowdMix.Tests   # unit tests + coverage
```

Quality gates enforced in CI: `dotnet format`, strict build (`TreatWarningsAsErrors`),
≥80% line coverage, CRAP ≤15, ≤250 lines/file, Gherkin acceptance tests.

## License

MIT.
