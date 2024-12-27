namespace GGNet.Rendering;

public static class SemaphoreSlimExtensions
{
  public static bool TryRelease(this SemaphoreSlim semaphore)
  {
    try
    {
      semaphore.Release();

      return true;
    }
    catch (Exception)
    {
      return false;
    }
  }
}