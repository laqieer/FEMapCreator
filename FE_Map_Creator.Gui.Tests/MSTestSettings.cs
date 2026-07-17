using Avalonia;
using Avalonia.Headless;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace FE_Map_Creator.Gui.Tests;

[TestClass]
public static class MSTestSettings
{
  [AssemblyInitialize]
  public static void Initialize(TestContext context)
  {
    AppBuilder.Configure<Application>()
      .UseHeadless(new AvaloniaHeadlessPlatformOptions
      {
        UseHeadlessDrawing = false,
      })
      .UseSkia()
      .SetupWithoutStarting();
  }
}
