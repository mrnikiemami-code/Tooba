using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// تست‌های کانتینری PostgreSQL/SpiceDB نباید موازی شوند؛ daemon داکر و DataSource با هم تداخل می‌کنند.
/// </summary>
[CollectionDefinition("PostgresSerial", DisableParallelization = true)]
public sealed class PostgresSerialCollection;
