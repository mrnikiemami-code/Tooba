using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-STOREFRONT-EF-LOGGING-RELEASE-178 — دائم‌سازی مرز «حجم لاگ» در مالک واقعی پیکربندی Host.
/// </summary>
/// <remarks>
/// <para>
/// این گارد یک قاعدهٔ معماری تازه نمی‌سازد؛ فقط پیکربندی موجود را قفل می‌کند تا سیل لاگ دستورهای
/// EF Core (<c>Microsoft.EntityFrameworkCore.Database.Command</c> در سطح Information) به‌طور
/// پیش‌فرض و از جمله در Development برنگردد، بدون خفه‌کردن خطاها/هشدارها.
/// </para>
/// <para>
/// سطح‌ها به‌صورت <b>لایه‌ای</b> حل می‌شوند (پایه سپس فایل محیط)، چون همان کاری است که Host در
/// زمان اجرا انجام می‌دهد؛ نه فقط خواندن یک فایل.
/// </para>
/// </remarks>
public sealed class HostEfLoggingConfigurationGuardTests
{
    private const string EfCategory = "Microsoft.EntityFrameworkCore";

    private static readonly string[] LevelOrder =
    [
        "Trace", "Debug", "Information", "Warning", "Error", "Critical", "None",
    ];

    [Fact]
    public void Production_default_suppresses_ef_command_logging_and_keeps_app_diagnostics()
    {
        var levels = Resolve("appsettings.Production.json");

        // سیل لاگ: دستهٔ EF هرگز پایین‌تر از Warning نمی‌آید؛ خطا/هشدار EF می‌ماند.
        Assert.True(
            IsAtLeast(levels[EfCategory], "Warning"),
            $"production EF category must be >= Warning but was '{levels[EfCategory]}'");

        // تشخیص‌های خود برنامه حذف نشده‌اند و ASP.NET در سطح هشدار می‌ماند.
        Assert.Equal("Information", levels["Default"]);
        Assert.Equal("Warning", levels["Microsoft.AspNetCore"]);
    }

    [Fact]
    public void Development_default_also_suppresses_ef_command_logging()
    {
        var levels = Resolve("appsettings.Development.json");

        // حتی با Default: Debug، دستهٔ EF صریحاً بالاتر نگه داشته می‌شود: بدون dump دستورها.
        Assert.True(
            IsAtLeast(levels[EfCategory], "Warning"),
            $"development EF category must be >= Warning but was '{levels[EfCategory]}'");
        Assert.Equal("Debug", levels["Default"]);
    }

    [Fact]
    public void Nothing_can_lower_the_ef_category_below_warning_by_default()
    {
        foreach (var environment in new[] { "appsettings.json", "appsettings.Development.json", "appsettings.Production.json" })
        {
            using var document = JsonDocument.Parse(Read("src/backend/Host/Tooba.Host/" + environment));
            if (!document.RootElement.TryGetProperty("Logging", out var logging)
                || !logging.TryGetProperty("LogLevel", out var levelSection))
            {
                continue;
            }

            foreach (var entry in levelSection.EnumerateObject())
            {
                if (!entry.Name.StartsWith(EfCategory, StringComparison.Ordinal))
                {
                    continue;
                }

                // هیچ کلید EF نازل‌تر از Warning مجاز نیست (Trace/Debug/Information).
                Assert.True(
                    IsAtLeast(entry.Value.GetString() ?? string.Empty, "Warning"),
                    $"{environment} sets '{entry.Name}' to '{entry.Value.GetString()}'");
            }
        }
    }

    [Fact]
    public void Blanket_disablement_is_forbidden_so_failures_stay_visible()
    {
        foreach (var environment in new[] { "appsettings.json", "appsettings.Development.json", "appsettings.Production.json" })
        {
            using var document = JsonDocument.Parse(Read("src/backend/Host/Tooba.Host/" + environment));
            var logging = document.RootElement.GetProperty("Logging");
            var levelSection = logging.GetProperty("LogLevel");

            // «Default» هرگز None/Critical نمی‌شود: خطاها و رویدادهای امنیتی باید ثبت شوند.
            // Debug در Development برای تشخیص خود برنامه مجاز است، ولی Critical/None در هیچ محیطی نه.
            var defaultLevel = levelSection.GetProperty("Default").GetString() ?? string.Empty;
            Assert.NotEqual("None", defaultLevel);
            Assert.NotEqual("Critical", defaultLevel);

            if (!environment.Contains("Development", StringComparison.Ordinal))
            {
                Assert.True(
                    IsAtLeast(defaultLevel, "Information"),
                    $"{environment} lowers Default below Information");
            }

            foreach (var entry in levelSection.EnumerateObject())
            {
                var value = entry.Value.GetString() ?? string.Empty;
                Assert.NotEqual("None", value);
            }
        }
    }

    [Fact]
    public void Diagnostic_opt_in_is_configuration_only_not_a_hard_coded_filter()
    {
        // Host هیچ SetMinimumLevel/AddFilter دستی ندارد؛ پس تنها راه روشن‌کردن موقت تشخیص،
        // بازنویسی همین کلید پیکربندی (مثلاً Logging__LogLevel__Microsoft.EntityFrameworkCore)
        // است که در محیط فرایند قابل ردیابی است.
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");

        Assert.Contains("builder.Logging.AddJsonConsole(", program, StringComparison.Ordinal);
        Assert.Contains("builder.Logging.ClearProviders();", program, StringComparison.Ordinal);
        Assert.DoesNotContain("SetMinimumLevel", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddFilter(", program, StringComparison.Ordinal);
        Assert.DoesNotContain("LogLevel.", program, StringComparison.Ordinal);

        // کلید قابل بازنویسی همان مسیر استاندارد پیکربندی لاگ است.
        Assert.Matches(
            new Regex("\"Microsoft\\.EntityFrameworkCore\"\\s*:\\s*\"Warning\"", RegexOptions.None),
            Read("src/backend/Host/Tooba.Host/appsettings.json"));
    }

    [Fact]
    public void Sensitive_data_logging_and_detailed_errors_stay_disabled_for_every_module_context()
    {
        var npgsql = Read("src/backend/BuildingBlocks/Tooba.Persistence/ToobaNpgsql.cs");

        // پارامترها/مقادیر SQL هرگز لاگ نمی‌شوند و خطاهای تفصیلی EF هم خاموش می‌مانند.
        Assert.Contains("options.EnableSensitiveDataLogging(false);", npgsql, StringComparison.Ordinal);
        Assert.Contains("options.EnableDetailedErrors(false);", npgsql, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableSensitiveDataLogging(true)", npgsql, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableDetailedErrors(true)", npgsql, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- helpers

    private static string Read(string relative)
        => File.ReadAllText(Repo(relative));

    /// <summary>سطح مؤثر یک دسته را با همان قواعد لایه‌بندی Host حل می‌کند (پایه ← محیط).</summary>
    private static IReadOnlyDictionary<string, string> Resolve(string environmentFile)
    {
        var merged = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in new[] { "appsettings.json", environmentFile })
        {
            using var document = JsonDocument.Parse(Read("src/backend/Host/Tooba.Host/" + file));
            foreach (var entry in document.RootElement.GetProperty("Logging").GetProperty("LogLevel").EnumerateObject())
            {
                merged[entry.Name] = entry.Value.GetString() ?? string.Empty;
            }
        }

        return new Dictionary<string, string>(merged, StringComparer.Ordinal)
        {
            [EfCategory] = ResolveCategory(merged, EfCategory),
        };
    }

    /// <summary>قانون ASP.NET Core: طولانی‌ترین پیشوند منطبق برنده است، وگرنه Default.</summary>
    private static string ResolveCategory(IReadOnlyDictionary<string, string> levels, string category)
    {
        var best = string.Empty;
        foreach (var key in levels.Keys)
        {
            if (key == "Default")
            {
                continue;
            }

            var matches = category.Equals(key, StringComparison.Ordinal)
                          || category.StartsWith(key + ".", StringComparison.Ordinal);
            if (matches && key.Length > best.Length)
            {
                best = key;
            }
        }

        if (best.Length > 0)
        {
            return levels[best];
        }

        return levels.TryGetValue("Default", out var fallback) ? fallback : "Information";
    }

    private static bool IsAtLeast(string level, string minimum)
    {
        var index = Array.IndexOf(LevelOrder, level);
        var minimumIndex = Array.IndexOf(LevelOrder, minimum);
        Assert.True(index >= 0, $"unknown log level '{level}'");
        return index >= minimumIndex;
    }

    private static string Repo(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
