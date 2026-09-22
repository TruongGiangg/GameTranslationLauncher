using GameTranslationLauncher.Application.Installation;

namespace GameTranslationLauncher.Application.Tests;

[TestClass]
public sealed class InstallationOperationLockTests
{
    [TestMethod]
    public void TryAcquire_SameInstallation_IsExclusiveUntilReleased()
    {
        var operationLock = new InstallationOperationLock();
        var installationId = Guid.NewGuid();
        using var firstLease = operationLock.TryAcquire("sample-game", installationId);

        var concurrentLease = operationLock.TryAcquire("SAMPLE-GAME", installationId);

        Assert.IsNotNull(firstLease);
        Assert.IsNull(concurrentLease);
        firstLease.Dispose();
        using var nextLease = operationLock.TryAcquire("sample-game", installationId);
        Assert.IsNotNull(nextLease);
    }
}
