using FEXNA_Library;
using FE_Map_Creator.Generation;

namespace FE_Map_Creator.Tests;

[TestClass]
public sealed class MapGenerationEngineTests
{
  [TestMethod]
  public void GenerateFillsBlankMapAndReportsSeed()
  {
    Map_Generation_Engine engine = new Map_Generation_Engine(create_uniform_data(1));
    Map_State state = blank_state(4, 3);

    Map_Generation_Result result = engine.generate(state, new Map_Generation_Options()
    {
      Depth = 1,
      Seed = 1234
    });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.AreEqual(1234, result.Seed);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void GenerateProgressCountsSeedAndEveryNewCell()
  {
    Map_State state = blank_state(2, 2);
    Recording_Progress progress = new Recording_Progress();

    new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 1234
      },
      progress: progress);

    CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, progress.Values);
  }

  [TestMethod]
  public void GenerateWithSameSeedIsDeterministic()
  {
    Tileset_Generation_Data data = create_connected_data(1, 2);
    Map_State first = blank_state(6, 5);
    Map_State second = blank_state(6, 5);

    new Map_Generation_Engine(data).generate(first, new Map_Generation_Options()
    {
      Depth = 1,
      Seed = 42
    });
    new Map_Generation_Engine(data).generate(second, new Map_Generation_Options()
    {
      Depth = 1,
      Seed = 42
    });

    CollectionAssert.AreEqual(first.Tiles.Cast<int>().ToArray(), second.Tiles.Cast<int>().ToArray());
  }

  [TestMethod]
  public void GenerateWithFixedSeedPreservesStableOutput()
  {
    Map_State state = blank_state(6, 5);

    new Map_Generation_Engine(create_connected_data(1, 2)).generate(state, new Map_Generation_Options()
    {
      Depth = 1,
      Seed = 42
    });

    CollectionAssert.AreEqual(
      new int[]
      {
        1, 1, 1, 2, 1, 2,
        2, 1, 2, 1, 1, 1,
        2, 1, 2, 1, 2, 1,
        2, 1, 1, 1, 1, 2,
        1, 1, 2, 2, 1, 2
      },
      tiles_row_major(state));
  }

  [TestMethod]
  public void IdenticalAliasIndexPreservesOrderingAndSeededOutput()
  {
    Map_State state = blank_state(8, 1);

    new Map_Generation_Engine(create_alias_data()).generate(state, new Map_Generation_Options()
    {
      Depth = 1,
      Seed = 77
    });

    CollectionAssert.AreEqual(new int[] { 1, 1, 3, 3, 2, 3, 3, 1 }, tiles_row_major(state));
  }

  [TestMethod]
  public void GenerateWithAllCellsDrawnAndLockedIsNoOp()
  {
    Map_State state = new Map_State(
      new int[2, 2]
      {
        { 1, 3 },
        { 2, 4 }
      },
      filled_bool_array(2, 2, true),
      filled_bool_array(2, 2, true),
      new int[2, 2]);
    int[] expected_tiles = state.Tiles.Cast<int>().ToArray();
    bool[] expected_drawn = state.Drawn.Cast<bool>().ToArray();
    bool[] expected_locked = state.Locked.Cast<bool>().ToArray();

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 5
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    CollectionAssert.AreEqual(expected_tiles, state.Tiles.Cast<int>().ToArray());
    CollectionAssert.AreEqual(expected_drawn, state.Drawn.Cast<bool>().ToArray());
    CollectionAssert.AreEqual(expected_locked, state.Locked.Cast<bool>().ToArray());
  }

  [TestMethod]
  public void GeneratePreservesLockedTemplateTile()
  {
    Map_State state = blank_state(3, 3);
    state.Tiles[1, 1] = 1;
    state.Drawn[1, 1] = true;
    state.Locked[1, 1] = true;

    new Map_Generation_Engine(create_uniform_data(1)).generate(state, new Map_Generation_Options()
    {
      Seed = 7
    });

    Assert.AreEqual(1, state.Tiles[1, 1]);
    Assert.IsTrue(state.Locked[1, 1]);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void GenerateFillsDisconnectedRegionsSeparatedByLockedCell()
  {
    Map_State state = blank_state(5, 1);
    state.Tiles[0, 0] = 1;
    state.Drawn[0, 0] = true;
    state.Tiles[2, 0] = 1;
    state.Drawn[2, 0] = true;
    state.Locked[2, 0] = true;

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 6
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.AreEqual(1, state.Tiles[2, 0]);
    Assert.IsTrue(state.Locked[2, 0]);
    Assert.AreEqual(1, state.Tiles[1, 0]);
    Assert.AreEqual(1, state.Tiles[3, 0]);
    Assert.AreEqual(1, state.Tiles[4, 0]);
    Assert.IsTrue(state.Drawn[4, 0]);
  }

  [TestMethod]
  public void GenerateDisconnectedRegionsPreservesSeededSelectionOrder()
  {
    Map_State state = isolated_checkerboard_state(5, 5);

    Map_Generation_Result result = new Map_Generation_Engine(create_connected_data(1, 2)).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 42
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    CollectionAssert.AreEqual(
      new int[]
      {
        2, 1, 1, 1, 1,
        1, 1, 1, 1, 1,
        1, 1, 1, 1, 1,
        1, 2, 1, 1, 1,
        2, 1, 1, 1, 1
      },
      tiles_row_major(state));
  }

  [TestMethod]
  public void GenerateHandlesManyIsolatedRegions()
  {
    Map_State state = isolated_checkerboard_state(40, 40);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 43
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void TerrainConstraintFiltersCandidates()
  {
    Tileset_Generation_Data data = create_connected_data(1, 2);
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1, 2 }
    };
    Map_State state = blank_state(3, 3);
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
        state.Terrain[x, y] = 2;
    }

    new Map_Generation_Engine(data, metadata).generate(state, new Map_Generation_Options()
    {
      Seed = 8
    });

    assert_all_tiles(state, 2);
  }

  [TestMethod]
  public void NegativeTerrainConstraintExcludesForbiddenCandidates()
  {
    Tileset_Generation_Data data = create_connected_data(1, 2);
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1, 2 }
    };
    Map_State state = blank_state(3, 3);
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
        state.Terrain[x, y] = -1;
    }

    new Map_Generation_Engine(data, metadata).generate(state, new Map_Generation_Options()
    {
      Seed = 8
    });

    assert_all_tiles(state, 2);
  }

  [TestMethod]
  public void RepairPreservesLockedCellsAndFillsHole()
  {
    int[,] tiles = new int[3, 1]
    {
      { 1 },
      { 0 },
      { 1 }
    };
    bool[,] drawn = new bool[3, 1]
    {
      { true },
      { true },
      { true }
    };
    bool[,] locked = new bool[3, 1]
    {
      { true },
      { false },
      { false }
    };
    Map_State state = new Map_State(tiles, drawn, locked, new int[3, 1]);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).repair(
      state,
      new Map_Repair_Options()
      {
        Radius = 1,
        Depth = 1,
        Seed = 9
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.AreEqual(1, state.Tiles[0, 0]);
    Assert.IsTrue(state.Locked[0, 0]);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void RepairPreservesLockedTerrainIncompatibleCell()
  {
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1 }
    };
    Map_State state = new Map_State(
      new int[2, 1]
      {
        { 1 },
        { 1 }
      },
      filled_bool_array(2, 1, true),
      new bool[2, 1]
      {
        { true },
        { false }
      },
      new int[2, 1]
      {
        { 2 },
        { 2 }
      });

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1), metadata).repair(
      state,
      new Map_Repair_Options()
      {
        Radius = 0,
        Depth = 1,
        Seed = 15
      });

    Assert.AreEqual(1, result.Unresolved_Tile_Count);
    Assert.AreEqual(1, state.Tiles[0, 0]);
    Assert.IsTrue(state.Drawn[0, 0]);
    Assert.IsTrue(state.Locked[0, 0]);
    Assert.AreEqual(0, state.Tiles[1, 0]);
    Assert.IsTrue(state.Drawn[1, 0]);
    Assert.IsFalse(state.Locked[1, 0]);
  }

  [TestMethod]
  public void RepairUsesLockedZeroAsHoleOriginWithoutChangingIt()
  {
    Map_State state = new Map_State(
      new int[2, 1]
      {
        { 0 },
        { 1 }
      },
      filled_bool_array(2, 1, true),
      new bool[2, 1]
      {
        { true },
        { false }
      },
      new int[2, 1]);
    int redrawn = 0;

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).repair(
      state,
      new Map_Repair_Options()
      {
        Radius = 1,
        Depth = 1,
        Seed = 16
      },
      tile_drawn: (x, y, tile) => ++redrawn);

    Assert.AreEqual(1, result.Unresolved_Tile_Count);
    Assert.AreEqual(0, state.Tiles[0, 0]);
    Assert.IsTrue(state.Drawn[0, 0]);
    Assert.IsTrue(state.Locked[0, 0]);
    Assert.AreEqual(0, state.Tiles[1, 0]);
    Assert.IsTrue(state.Drawn[1, 0]);
    Assert.IsFalse(state.Locked[1, 0]);
    Assert.AreEqual(1, redrawn);
  }

  [TestMethod]
  public void GenerateWithEmptyConfigMarksEveryCellUnresolved()
  {
    Map_State state = blank_state(2, 2);
    Recording_Progress progress = new Recording_Progress();

    Map_Generation_Result result = new Map_Generation_Engine(create_generation_data()).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 12
      },
      progress: progress);

    Assert.AreEqual(4, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 0);
    CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, progress.Values);
  }

  [TestMethod]
  public void GenerateWithImpossibleTerrainMarksEveryCellUnresolved()
  {
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1, 2 }
    };
    Map_State state = blank_state(2, 2);
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
        state.Terrain[x, y] = 999;
    }

    Map_Generation_Result result = new Map_Generation_Engine(create_connected_data(1, 2), metadata).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 13
      });

    Assert.AreEqual(4, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 0);
  }

  [TestMethod]
  public void GenerateMarksOnlyImpossibleSeedCellUnresolved()
  {
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1 }
    };
    Map_State state = blank_state(2, 1);
    state.Terrain[0, 0] = 999;

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1), metadata).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 1
      });

    Assert.AreEqual(1, result.Unresolved_Tile_Count);
    Assert.AreEqual(0, state.Tiles[0, 0]);
    Assert.IsTrue(state.Drawn[0, 0]);
    Assert.AreEqual(1, state.Tiles[1, 0]);
    Assert.IsTrue(state.Drawn[1, 0]);
  }

  [TestMethod]
  public void GenerateReportsUnresolvedCells()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = new Tile_Data();
    Map_State state = blank_state(2, 1);

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 10
      });

    Assert.AreEqual(1, result.Unresolved_Tile_Count);
    Assert.IsTrue(state.Drawn[0, 0]);
    Assert.IsTrue(state.Drawn[1, 0]);
    Assert.IsTrue(state.Tiles[0, 0] == 0 || state.Tiles[1, 0] == 0);
  }

  [TestMethod]
  public void DepthTwoLookaheadAvoidsDepthOneDeadEnd()
  {
    Map_State depth_one = depth_fixture_state();
    Map_State depth_two = depth_fixture_state();

    Map_Generation_Result depth_one_result = new Map_Generation_Engine(create_depth_fixture_data()).generate(
      depth_one,
      new Map_Generation_Options()
      {
        Depth = 1,
        Seed = 0
      });
    Map_Generation_Result depth_two_result = new Map_Generation_Engine(create_depth_fixture_data()).generate(
      depth_two,
      new Map_Generation_Options()
      {
        Depth = 2,
        Seed = 0
      });

    Assert.AreEqual(2, depth_one_result.Unresolved_Tile_Count);
    CollectionAssert.AreEqual(new int[] { 1, 2, 0, 0 }, tiles_row_major(depth_one));
    Assert.AreEqual(0, depth_two_result.Unresolved_Tile_Count);
    CollectionAssert.AreEqual(new int[] { 1, 3, 5, 6 }, tiles_row_major(depth_two));
    assert_horizontal_adjacency(create_depth_fixture_data(), depth_two);
  }

  [TestMethod]
  public void ExperimentalDepthOneBacktracksAcrossLegacyDeadEnd()
  {
    Tileset_Generation_Data data = create_depth_fixture_data();
    Map_State state = depth_fixture_state();

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Depth = 1,
        Seed = 0
      });

    Assert.AreEqual(Map_Generation_Algorithm.Experimental_Constraint, result.Algorithm);
    Assert.IsTrue(result.Is_Complete);
    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    assert_horizontal_adjacency(data, state);
  }

  [TestMethod]
  public void ExperimentalReportsExhaustedSearchBudget()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = new Tile_Data();
    data.generation_data[2] = new Tile_Data();
    Map_State state = blank_state(2, 1);

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Depth = 1,
        Experimental_Search_Node_Limit = 1,
        Seed = 0
      });

    Assert.IsTrue(result.Search_Budget_Exhausted);
    Assert.AreEqual(1, result.Search_Node_Count);
  }

  [TestMethod]
  public void ExperimentalRejectsNonPositiveSearchBudget()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() =>
      new Map_Generation_Engine(create_uniform_data(1)).generate(
        blank_state(1, 1),
        new Map_Generation_Options()
        {
          Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
          Experimental_Search_Node_Limit = 0
        }));
  }

  [TestMethod]
  public void ExperimentalImpossibleTerrainReturnsMinimumOpenCells()
  {
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1, 2 }
    };
    Map_State state = blank_state(2, 2);
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
        state.Terrain[x, y] = 999;
    }

    Map_Generation_Result result = new Map_Generation_Engine(create_connected_data(1, 2), metadata).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Seed = 13
      });

    Assert.AreEqual(4, result.Unresolved_Tile_Count);
    Assert.HasCount(4, result.Unresolved_Cells);
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        Assert.AreEqual(0, state.Tiles[x, y]);
        Assert.IsFalse(state.Drawn[x, y]);
      }
    }
  }

  [TestMethod]
  public void ExperimentalTreatsTileZeroAsValidCandidate()
  {
    Map_State state = blank_state(2, 2);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(0)).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Seed = 17
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 0);
  }

  [TestMethod]
  public void ExperimentalDeduplicatesRedundantGenerationKeys()
  {
    Tileset_Generation_Data data = create_alias_data();
    data.generation_data[2] = new Tile_Data(data.generation_data[1]);
    Map_State state = blank_state(4, 1);

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Seed = 22
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.IsTrue(state.Drawn.Cast<bool>().All(drawn => drawn));
  }

  [TestMethod]
  public void ExperimentalMinimizesStructuralDeadEndToOneCell()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = create_tile_data((6, 2, 1));
    data.generation_data[2] = create_tile_data((4, 1, 1));
    Map_State state = blank_state(3, 1);
    state.Tiles[0, 0] = 1;
    state.Drawn[0, 0] = true;
    state.Locked[0, 0] = true;

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Seed = 21
      });

    Assert.AreEqual(1, result.Unresolved_Tile_Count);
    Assert.HasCount(1, result.Unresolved_Cells);
    Assert.AreEqual(1, state.Drawn.Cast<bool>().Count(drawn => !drawn));
  }

  [TestMethod]
  public void ExperimentalReportsAndSolvesDisconnectedComponents()
  {
    Map_State state = isolated_checkerboard_state(5, 5);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = 13,
        Seed = 24
      });

    Assert.AreEqual(13, result.Search_Component_Count);
    Assert.HasCount(13, result.Components);
    Assert.AreEqual(result.Search_Node_Count, result.Components.Sum(component => component.Search_Node_Count));
    Assert.AreEqual(
      result.Unresolved_Tile_Count,
      result.Components.Sum(component => component.Unresolved_Tile_Count));
    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.IsLessThanOrEqualTo(13, result.Search_Node_Count);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void ExperimentalComponentBudgetsNeverExceedTotalLimit()
  {
    Map_State state = isolated_checkerboard_state(5, 5);

    Map_Generation_Result result = new Map_Generation_Engine(create_generation_data()).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = 5,
        Seed = 25
      });

    Assert.AreEqual(13, result.Search_Component_Count);
    Assert.AreEqual(13, result.Unresolved_Tile_Count);
    Assert.IsTrue(result.Search_Budget_Exhausted);
    Assert.IsLessThanOrEqualTo(5, result.Search_Node_Count);
  }

  [TestMethod]
  public void ExperimentalCarriesUnusedBudgetToLaterComponent()
  {
    Map_State state = blank_state(6, 1);
    state.Tiles[1, 0] = 99;
    state.Drawn[1, 0] = true;
    state.Locked[1, 0] = true;
    state.Tiles[2, 0] = 1;
    state.Drawn[2, 0] = true;
    state.Locked[2, 0] = true;

    Map_Generation_Result result = new Map_Generation_Engine(create_depth_fixture_data()).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = 100,
        Seed = 28
      });

    Assert.AreEqual(2, result.Search_Component_Count);
    Assert.AreEqual(0, result.Components[0].Search_Node_Count);
    Assert.AreEqual(100, result.Components[1].Search_Node_Limit);
    Assert.IsLessThanOrEqualTo(100, result.Search_Node_Count);
    Assert.AreEqual(
      0,
      result.Unresolved_Tile_Count,
      string.Join(
        "; ",
        result.Components.Select(component =>
          $"{component.Origin.X},{component.Origin.Y}: nodes={component.Search_Node_Count}, unresolved={component.Unresolved_Tile_Count}, exhausted={component.Search_Budget_Exhausted}, propagation={component.Propagation_Removal_Count}")));
  }

  [TestMethod]
  public void ExperimentalPropagationPrunesUnsupportedDomains()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = new Tile_Data();
    data.generation_data[2] = new Tile_Data();
    Map_State state = blank_state(2, 1);

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = 2,
        Seed = 26
      });

    Assert.IsGreaterThan(0, result.Propagation_Removal_Count);
    Assert.AreEqual(1, result.Search_Component_Count);
    Assert.IsLessThanOrEqualTo(2, result.Search_Node_Count);
  }

  [TestMethod]
  public void ExperimentalComponentProgressIsMonotonic()
  {
    Map_State state = isolated_checkerboard_state(5, 5);
    Recording_Progress progress = new Recording_Progress();

    new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Seed = 27
      },
      progress: progress);

    Assert.IsNotEmpty(progress.Values);
    for (int index = 1; index < progress.Values.Count; ++index)
      Assert.IsGreaterThanOrEqualTo(progress.Values[index - 1], progress.Values[index]);
    Assert.AreEqual(13, progress.Values[^1]);
  }

  [TestMethod]
  public void ExperimentalWithSameSeedIsDeterministic()
  {
    Tileset_Generation_Data data = create_connected_data(1, 2);
    Map_State first = blank_state(6, 5);
    Map_State second = blank_state(6, 5);
    Map_Generation_Options options = new Map_Generation_Options()
    {
      Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
      Depth = 2,
      Seed = 42
    };

    new Map_Generation_Engine(data).generate(first, options);
    new Map_Generation_Engine(data).generate(second, options);

    CollectionAssert.AreEqual(first.Tiles.Cast<int>().ToArray(), second.Tiles.Cast<int>().ToArray());
    CollectionAssert.AreEqual(first.Drawn.Cast<bool>().ToArray(), second.Drawn.Cast<bool>().ToArray());
  }

  [TestMethod]
  public void ExperimentalRepairFillsHole()
  {
    Map_State state = new Map_State(
      new int[3, 1]
      {
        { 1 },
        { 0 },
        { 1 }
      },
      new bool[3, 1]
      {
        { true },
        { false },
        { true }
      },
      new bool[3, 1],
      new int[3, 1]);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).repair(
      state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Radius = 1,
        Depth = 1,
        Seed = 18
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void ExperimentalRepairPreservesDrawnTileZero()
  {
    Map_State state = new Map_State(
      new int[3, 1]
      {
        { 0 },
        { 0 },
        { 0 }
      },
      filled_bool_array(3, 1, true),
      new bool[3, 1],
      new int[3, 1]);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(0)).repair(
      state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Radius = 1,
        Seed = 20
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 0);
  }

  [TestMethod]
  public void ExperimentalRepairUsesLockedOpenZeroAsHoleOrigin()
  {
    Map_State state = new Map_State(
      new int[2, 1]
      {
        { 0 },
        { 1 }
      },
      new bool[2, 1]
      {
        { false },
        { true }
      },
      new bool[2, 1]
      {
        { true },
        { false }
      },
      new int[2, 1]);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).repair(
      state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Radius = 1,
        Seed = 23
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.AreEqual(0, state.Tiles[0, 0]);
    Assert.IsTrue(state.Drawn[0, 0]);
    Assert.IsTrue(state.Locked[0, 0]);
    Assert.AreEqual(1, state.Tiles[1, 0]);
    Assert.IsTrue(state.Drawn[1, 0]);
  }

  [TestMethod]
  public void ExperimentalCancellationDoesNotMutateState()
  {
    using CancellationTokenSource cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    Map_State state = blank_state(3, 3);
    int[] expected_tiles = state.Tiles.Cast<int>().ToArray();
    bool[] expected_drawn = state.Drawn.Cast<bool>().ToArray();

    Assert.Throws<OperationCanceledException>(() =>
      new Map_Generation_Engine(create_uniform_data(1)).generate(
        state,
        new Map_Generation_Options()
        {
          Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
          Seed = 19
        },
        cancellation.Token));

    CollectionAssert.AreEqual(expected_tiles, state.Tiles.Cast<int>().ToArray());
    CollectionAssert.AreEqual(expected_drawn, state.Drawn.Cast<bool>().ToArray());
  }

  [TestMethod]
  public void GenerateHonorsCancellation()
  {
    using CancellationTokenSource cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    Map_State state = blank_state(3, 3);
    int[] expected_tiles = state.Tiles.Cast<int>().ToArray();
    bool[] expected_drawn = state.Drawn.Cast<bool>().ToArray();

    Assert.Throws<OperationCanceledException>(() =>
      new Map_Generation_Engine(create_uniform_data(1)).generate(
        state,
        new Map_Generation_Options()
        {
          Seed = 11
        },
        cancellation.Token));

    CollectionAssert.AreEqual(expected_tiles, state.Tiles.Cast<int>().ToArray());
    CollectionAssert.AreEqual(expected_drawn, state.Drawn.Cast<bool>().ToArray());
  }

  [TestMethod]
  public void RepairHonorsCancellationWithoutMutatingState()
  {
    using CancellationTokenSource cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    Map_State state = new Map_State(
      new int[3, 1]
      {
        { 1 },
        { 0 },
        { 1 }
      },
      filled_bool_array(3, 1, true),
      new bool[3, 1],
      new int[3, 1]);
    int[] expected_tiles = state.Tiles.Cast<int>().ToArray();
    bool[] expected_drawn = state.Drawn.Cast<bool>().ToArray();

    Assert.Throws<OperationCanceledException>(() =>
      new Map_Generation_Engine(create_uniform_data(1)).repair(
        state,
        new Map_Repair_Options()
        {
          Radius = 1,
          Depth = 1,
          Seed = 14
        },
        cancellation.Token));

    CollectionAssert.AreEqual(expected_tiles, state.Tiles.Cast<int>().ToArray());
    CollectionAssert.AreEqual(expected_drawn, state.Drawn.Cast<bool>().ToArray());
  }

  private static Map_State blank_state(int width, int height)
  {
    return new Map_State(
      new int[width, height],
      new bool[width, height],
      new bool[width, height],
      new int[width, height]);
  }

  private static Tileset_Generation_Data create_uniform_data(short tile)
  {
    Tileset_Generation_Data data = create_generation_data();
    Tile_Data tile_data = new Tile_Data();
    foreach (byte direction in new byte[] { 2, 4, 6, 8 })
      tile_data.Valid_Tile_Priority[direction][tile] = 1;
    data.generation_data[tile] = tile_data;
    return data;
  }

  private static Tileset_Generation_Data create_alias_data()
  {
    Tile_Matching_Data matching = new Tile_Matching_Data(new HashSet<Tile_Directions>()
    {
      Tile_Directions.Center
    });
    matching.add(Tile_Directions.Center, new List<short>() { 1, 2, 3 });
    matching.refresh_identical(4);
    Tileset_Generation_Data data = new Tileset_Generation_Data(4, matching);
    Tile_Data tile_data = new Tile_Data();
    foreach (byte direction in new byte[] { 2, 4, 6, 8 })
      tile_data.Valid_Tile_Priority[direction][1] = 1;
    data.generation_data[1] = tile_data;
    return data;
  }

  private static Map_State depth_fixture_state()
  {
    Map_State state = blank_state(4, 1);
    state.Tiles[0, 0] = 1;
    state.Drawn[0, 0] = true;
    state.Locked[0, 0] = true;
    return state;
  }

  private static Map_State isolated_checkerboard_state(int width, int height)
  {
    Map_State state = blank_state(width, height);
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
      {
        if ((x + y) % 2 == 1)
        {
          state.Tiles[x, y] = 1;
          state.Drawn[x, y] = true;
          state.Locked[x, y] = true;
        }
      }
    }
    return state;
  }

  private static Tileset_Generation_Data create_depth_fixture_data()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = create_tile_data((6, 2, 100), (6, 3, 1));
    data.generation_data[2] = create_tile_data((4, 1, 1), (6, 4, 1));
    data.generation_data[3] = create_tile_data((4, 1, 1), (6, 5, 1));
    data.generation_data[4] = create_tile_data((4, 2, 1));
    data.generation_data[5] = create_tile_data((4, 3, 1), (6, 6, 1));
    data.generation_data[6] = create_tile_data((4, 5, 1));
    return data;
  }

  private static Tile_Data create_tile_data(params (byte Direction, short Tile, short Weight)[] edges)
  {
    Tile_Data data = new Tile_Data();
    foreach ((byte direction, short tile, short weight) in edges)
      data.Valid_Tile_Priority[direction][tile] = weight;
    return data;
  }

  private static Tileset_Generation_Data create_connected_data(params short[] tiles)
  {
    Tileset_Generation_Data data = create_generation_data();
    foreach (short tile in tiles)
    {
      Tile_Data tile_data = new Tile_Data();
      foreach (byte direction in new byte[] { 2, 4, 6, 8 })
      {
        foreach (short neighbor in tiles)
          tile_data.Valid_Tile_Priority[direction][neighbor] = 1;
      }
      data.generation_data[tile] = tile_data;
    }
    return data;
  }

  private static Tileset_Generation_Data create_generation_data()
  {
    return new Tileset_Generation_Data(
      0,
      new Tile_Matching_Data(new HashSet<Tile_Directions>()));
  }

  private static bool[,] filled_bool_array(int width, int height, bool value)
  {
    bool[,] result = new bool[width, height];
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
        result[x, y] = value;
    }
    return result;
  }

  private static int[] tiles_row_major(Map_State state)
  {
    List<int> result = new List<int>();
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
        result.Add(state.Tiles[x, y]);
    }
    return result.ToArray();
  }

  private static void assert_horizontal_adjacency(Tileset_Generation_Data data, Map_State state)
  {
    for (int x = 0; x + 1 < state.Width; ++x)
    {
      short left = (short) state.Tiles[x, 0];
      short right = (short) state.Tiles[x + 1, 0];
      Assert.IsTrue(data.generation_data[left].Valid_Tile_Priority[(byte) 6].ContainsKey(right));
      Assert.IsTrue(data.generation_data[right].Valid_Tile_Priority[(byte) 4].ContainsKey(left));
    }
  }

  private static void assert_all_tiles(Map_State state, int expected)
  {
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        Assert.IsTrue(state.Drawn[x, y], $"Cell ({x},{y}) was not drawn.");
        Assert.AreEqual(expected, state.Tiles[x, y], $"Unexpected tile at ({x},{y}).");
      }
    }
  }

  private sealed class Recording_Progress : IProgress<int>
  {
    public List<int> Values { get; } = new List<int>();

    public void Report(int value)
    {
      this.Values.Add(value);
    }
  }
}
