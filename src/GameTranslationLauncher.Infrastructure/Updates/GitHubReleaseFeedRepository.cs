using System.Net.Http.Headers;
using System.Text.Json;
using GameTranslationLauncher.Application.Updates;
using GameTranslationLauncher.Domain.Updates;

namespace GameTranslationLauncher.Infrastructure.Updates;

/// <summary>
/// Đọc bản phát hành mới nhất của Launcher từ GitHub Releases (API công khai, không cần token
/// vì repo dùng để phát hành có thể để public hoặc release vẫn đọc được qua token cài sẵn).
/// </summary>
public sealed class GitHubReleaseFeedRepository : IUpdateFeedRepository
{
    private static readonly HttpClient HttpClient = CreateHttpClient();
    private static readonly char[] SeparatorChars = [' ', '\t', '\r', '\n'];

    private readonly string owner;
    private readonly string repository;

    public GitHubReleaseFeedRepository(string owner, string repository)
    {
        this.owner = owner;
        this.repository = repository;
    }

    public async Task<LauncherReleaseInfo?> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(
            $"https://api.github.com/repos/{owner}/{repository}/releases/latest",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (!root.TryGetProperty("tag_name", out var tagNameElement)
            || !TryParseVersion(tagNameElement.GetString(), out var version))
        {
            return null;
        }

        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        string? downloadUrl = null;
        string? checksumAssetUrl = null;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
            var url = asset.TryGetProperty("browser_download_url", out var urlElement) ? urlElement.GetString() : null;
            if (name is null || url is null)
            {
                continue;
            }

            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                downloadUrl = url;
            }
            else if (name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
            {
                checksumAssetUrl = url;
            }
        }

        if (downloadUrl is null)
        {
            return null;
        }

        var sha256 = checksumAssetUrl is null
            ? null
            : await TryReadChecksumAsync(checksumAssetUrl, cancellationToken);
        var htmlUrl = root.TryGetProperty("html_url", out var htmlUrlElement)
            ? htmlUrlElement.GetString() ?? string.Empty
            : string.Empty;
        var releaseNotes = root.TryGetProperty("body", out var bodyElement)
            ? bodyElement.GetString() ?? string.Empty
            : string.Empty;

        return new LauncherReleaseInfo(version, downloadUrl, sha256, htmlUrl, releaseNotes);
    }

    private static async Task<string?> TryReadChecksumAsync(string checksumAssetUrl, CancellationToken cancellationToken)
    {
        try
        {
            var content = await HttpClient.GetStringAsync(checksumAssetUrl, cancellationToken);
            // Chấp nhận cả file chỉ chứa hash lẫn định dạng "sha256sum" ("<hash>  <filename>").
            var token = content.AsSpan().Trim();
            var separatorIndex = token.IndexOfAny(SeparatorChars);
            var hash = separatorIndex < 0 ? token : token[..separatorIndex];
            return hash.Length == 64 ? hash.ToString().ToLowerInvariant() : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private static bool TryParseVersion(string? tagName, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return false;
        }

        var candidate = tagName.StartsWith('v') || tagName.StartsWith('V')
            ? tagName[1..]
            : tagName;
        return Version.TryParse(candidate, out version!);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GameTranslationLauncher", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}
