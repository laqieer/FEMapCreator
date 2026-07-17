using FE_Map_Creator.Editing;
using FE_Map_Creator.Generation;

namespace FE_Map_Creator.Tests;

[TestClass]
public sealed class MapEditEngineTests
{
  [TestMethod]
  public void AppliesOrderedCellLockAndTerrainOperations()
  {
    Map_State original = empty_state(3, 2);
    Map_Edit_Engine engine = new Map_Edit_Engine();

    Map_State actual = engine.apply(original, new[]
    {
      new Map_Edit_Operation { Action = "set-tile", X = 1, Y = 1, Tile = 0 },
      new Map_Edit_Operation { Action = "lock", X = 1, Y = 1 },
      new Map_Edit_Operation { Action = "require-terrain", X = 1, Y = 1, Terrain = 12 },
      new Map_Edit_Operation { Action = "forbid-terrain", X = 2, Y = 0, Terrain = 4 },
      new Map_Edit_Operation { Action = "clear-terrain" },
      new Map_Edit_Operation { Action = "lock", X = 0, Y = 0 },
      new Map_Edit_Operation { Action = "clear-locks" },
    });

    Assert.IsFalse(original.Drawn[1, 1], "The input state must remain unchanged.");
    Assert.IsTrue(actual.Drawn[1, 1], "A drawn tile 0 must remain distinct from an open cell.");
    Assert.IsFalse(actual.Locked[1, 1]);
    Assert.AreEqual(0, actual.Terrain[1, 1]);
    Assert.AreEqual(0, actual.Terrain[2, 0]);
    Assert.IsFalse(actual.Locked[0, 0]);
  }

  [TestMethod]
  public void RectangleClipsAndFloodFillUsesFourWayConnectivity()
  {
    Map_State original = empty_state(4, 3);
    original.Tiles[1, 1] = 8;
    original.Drawn[1, 1] = true;
    Map_Edit_Engine engine = new Map_Edit_Engine();

    Map_State actual = engine.apply(original, new[]
    {
      new Map_Edit_Operation
      {
        Action = "set-tile",
        Shape = "rectangle",
        X = -2,
        Y = -1,
        EndX = 1,
        EndY = 0,
        Tile = 7
      },
      new Map_Edit_Operation
      {
        Action = "set-tile",
        Shape = "flood-fill",
        X = 3,
        Y = 2,
        Tile = 9
      }
    });

    Assert.AreEqual(7, actual.Tiles[0, 0]);
    Assert.AreEqual(7, actual.Tiles[1, 0]);
    Assert.AreEqual(8, actual.Tiles[1, 1]);
    Assert.AreEqual(9, actual.Tiles[3, 2]);
    Assert.AreEqual(9, actual.Tiles[0, 2]);
    Assert.AreEqual(9, actual.Tiles[2, 1]);
  }

  [TestMethod]
  public void ResizePreservesTopLeftOverlapAndMetadata()
  {
    Map_State original = empty_state(2, 2);
    original.Tiles[1, 1] = 15;
    original.Drawn[1, 1] = true;
    original.Locked[1, 1] = true;
    Map_Edit_Engine engine = new Map_Edit_Engine();

    Map_State actual = engine.apply(original, new[]
    {
      new Map_Edit_Operation { Action = "resize", Width = 4, Height = 3 }
    });

    Assert.AreEqual(4, actual.Width);
    Assert.AreEqual(3, actual.Height);
    Assert.AreEqual(15, actual.Tiles[1, 1]);
    Assert.IsTrue(actual.Drawn[1, 1]);
    Assert.IsTrue(actual.Locked[1, 1]);
    Assert.IsFalse(actual.Drawn[3, 2]);
  }

  [TestMethod]
  public void RejectsInvalidCoordinatesWithoutMutatingInput()
  {
    Map_State original = empty_state(2, 2);
    Map_Edit_Engine engine = new Map_Edit_Engine();

    InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
      engine.apply(original, new[]
      {
        new Map_Edit_Operation { Action = "set-tile", X = 0, Y = 0, Tile = 7 },
        new Map_Edit_Operation { Action = "erase", Shape = "flood-fill", X = 2, Y = 0 }
      }));

    StringAssert.Contains(exception.Message, "outside the 2x2 map");
    Assert.AreEqual(0, original.Tiles[0, 0]);
    Assert.IsFalse(original.Drawn[0, 0]);
  }

  [TestMethod]
  public void RejectsMalformedOperationParameters()
  {
    Map_Edit_Engine engine = new Map_Edit_Engine();
    Map_State state = empty_state(1, 1);

    Assert.Throws<InvalidOperationException>(() =>
      engine.apply(state, new[]
      {
        new Map_Edit_Operation { Action = "set-tile", X = 0, Y = 0, Tile = -1 }
      }));
    Assert.Throws<InvalidOperationException>(() =>
      engine.apply(state, new[]
      {
        new Map_Edit_Operation { Action = "require-terrain", X = 0, Y = 0, Terrain = 0 }
      }));
    Assert.Throws<InvalidOperationException>(() =>
      engine.apply(state, new[]
      {
        new Map_Edit_Operation { Action = "resize", Width = 2 }
      }));
  }

  private static Map_State empty_state(int width, int height)
  {
    return new Map_State(
      new int[width, height],
      new bool[width, height],
      new bool[width, height],
      new int[width, height]);
  }
}
