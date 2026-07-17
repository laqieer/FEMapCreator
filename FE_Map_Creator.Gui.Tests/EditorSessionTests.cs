using FE_Map_Creator.Editing;
using FE_Map_Creator.Generation;
using FE_Map_Creator.Gui.Models;

namespace FE_Map_Creator.Gui.Tests;

[TestClass]
public sealed class EditorSessionTests
{
  [TestMethod]
  public void PaintedTileZeroRemainsDistinctFromAnOpenCellAcrossUndo()
  {
    Editor_Session session = new Editor_Session(3, 2);

    session.begin_edit();
    session.apply_cell(1, 1, Editor_Mode.Tile, 0, 0);
    session.end_edit();

    Assert.AreEqual(0, session.Tiles[1, 1]);
    Assert.IsTrue(session.Drawn[1, 1]);
    Assert.IsTrue(session.undo());
    Assert.IsFalse(session.Drawn[1, 1]);
    Assert.IsTrue(session.redo());
    Assert.IsTrue(session.Drawn[1, 1]);
  }

  [TestMethod]
  public void LockAndTerrainEditsRemainMutuallyExclusive()
  {
    Editor_Session session = new Editor_Session(2, 2);
    session.begin_edit();
    session.apply_cell(0, 0, Editor_Mode.Tile, 4, 0);
    session.apply_cell(0, 0, Editor_Mode.Lock, 0, 0, true);
    session.end_edit();

    Assert.IsTrue(session.Locked[0, 0]);
    Assert.AreEqual(0, session.Terrain[0, 0]);

    session.begin_edit();
    session.apply_cell(0, 0, Editor_Mode.Terrain_Required, 0, 12);
    session.end_edit();

    Assert.IsFalse(session.Locked[0, 0]);
    Assert.AreEqual(12, session.Terrain[0, 0]);

    session.begin_edit();
    session.apply_cell(0, 0, Editor_Mode.Lock, 0, 0, true);
    session.end_edit();

    Assert.IsTrue(session.Locked[0, 0]);
    Assert.AreEqual(0, session.Terrain[0, 0]);
  }

  [TestMethod]
  public void RectangleAndFloodFillCreateSingleUndoableTransactions()
  {
    Editor_Session session = new Editor_Session(4, 3);
    session.begin_edit();
    session.apply_rectangle(
      new Map_Cell(0, 0),
      new Map_Cell(1, 2),
      Editor_Mode.Tile,
      7,
      0);
    session.end_edit();

    Assert.IsTrue(session.Drawn[1, 2]);
    Assert.IsFalse(session.Drawn[2, 2]);

    session.begin_edit();
    session.flood_fill(3, 1, Editor_Mode.Tile, 9, 0);
    session.end_edit();

    Assert.AreEqual(7, session.Tiles[0, 1]);
    Assert.AreEqual(9, session.Tiles[3, 1]);
    Assert.IsTrue(session.Drawn[2, 0]);

    Assert.IsTrue(session.undo());
    Assert.IsFalse(session.Drawn[3, 1]);
    Assert.IsTrue(session.Drawn[0, 1]);
    Assert.IsTrue(session.undo());
    Assert.IsFalse(session.Drawn[0, 1]);
  }

  [TestMethod]
  public void ResizePreservesOverlappingTileMetadata()
  {
    Editor_Session session = new Editor_Session(2, 2);
    session.begin_edit();
    session.apply_cell(1, 1, Editor_Mode.Tile, 15, 0);
    session.apply_cell(1, 1, Editor_Mode.Lock, 0, 0, true);
    session.end_edit();

    session.resize(4, 3);

    Assert.AreEqual(4, session.Width);
    Assert.AreEqual(3, session.Height);
    Assert.AreEqual(15, session.Tiles[1, 1]);
    Assert.IsTrue(session.Drawn[1, 1]);
    Assert.IsTrue(session.Locked[1, 1]);
    Assert.IsFalse(session.Drawn[3, 2]);
  }

  [TestMethod]
  public void CoreEditEngineMatchesGuiEditingSemantics()
  {
    Editor_Session session = new Editor_Session(4, 3);
    session.begin_edit();
    session.apply_rectangle(
      new Map_Cell(0, 0),
      new Map_Cell(1, 1),
      Editor_Mode.Tile,
      7,
      0);
    session.flood_fill(3, 2, Editor_Mode.Tile, 5, 0);
    session.apply_cell(0, 0, Editor_Mode.Lock, 0, 0, true);
    session.apply_cell(1, 1, Editor_Mode.Terrain_Required, 0, 12);
    session.apply_cell(3, 2, Editor_Mode.Terrain_Forbidden, 0, 4);
    session.end_edit();
    session.resize(5, 4);

    Map_State state = new Map_Edit_Engine().apply(
      new Map_State(
        new int[4, 3],
        new bool[4, 3],
        new bool[4, 3],
        new int[4, 3]),
      new[]
      {
        new Map_Edit_Operation
        {
          Action = "set-tile",
          Shape = "rectangle",
          X = 0,
          Y = 0,
          EndX = 1,
          EndY = 1,
          Tile = 7
        },
        new Map_Edit_Operation
        {
          Action = "set-tile",
          Shape = "flood-fill",
          X = 3,
          Y = 2,
          Tile = 5
        },
        new Map_Edit_Operation { Action = "lock", X = 0, Y = 0 },
        new Map_Edit_Operation { Action = "require-terrain", X = 1, Y = 1, Terrain = 12 },
        new Map_Edit_Operation { Action = "forbid-terrain", X = 3, Y = 2, Terrain = 4 },
        new Map_Edit_Operation { Action = "resize", Width = 5, Height = 4 }
      });

    Assert.AreEqual(session.Width, state.Width);
    Assert.AreEqual(session.Height, state.Height);
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        Assert.AreEqual(session.Tiles[x, y], state.Tiles[x, y], $"Tile mismatch at ({x},{y}).");
        Assert.AreEqual(session.Drawn[x, y], state.Drawn[x, y], $"Drawn mismatch at ({x},{y}).");
        Assert.AreEqual(session.Locked[x, y], state.Locked[x, y], $"Lock mismatch at ({x},{y}).");
        Assert.AreEqual(session.Terrain[x, y], state.Terrain[x, y], $"Terrain mismatch at ({x},{y}).");
      }
    }
  }
}
