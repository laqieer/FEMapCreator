using FEXNA_Library;
using FE_Map_Creator.Generation;

namespace FE_Map_Creator.Tests;

[TestClass]
public sealed class MapConstraintValidatorTests
{
  [TestMethod]
  public void ValidatesMutualAdjacencyAndTerrain()
  {
    Tileset_Generation_Data data = create_data();
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1, 2 }
    };
    int[,] tiles = new int[2, 1]
    {
      { 1 },
      { 2 }
    };
    int[,] terrain = new int[2, 1]
    {
      { 1 },
      { -1 }
    };

    Map_Validation_Result result = Map_Constraint_Validator.validate(
      tiles,
      data,
      metadata,
      terrain);

    Assert.IsTrue(result.Is_Valid);
    Assert.AreEqual(1, result.Checked_Adjacency_Count);
    Assert.AreEqual(0, result.Skipped_Zero_Cell_Count);
  }

  [TestMethod]
  public void ReportsInvalidAdjacencyAndTerrain()
  {
    Tileset_Generation_Data data = create_data();
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1, 2 }
    };
    int[,] tiles = new int[2, 1]
    {
      { 2 },
      { 1 }
    };
    int[,] terrain = new int[2, 1]
    {
      { 1 },
      { 0 }
    };

    Map_Validation_Result result = Map_Constraint_Validator.validate(
      tiles,
      data,
      metadata,
      terrain);

    Assert.IsFalse(result.Is_Valid);
    Assert.IsTrue(result.Errors.Any(error => error.Contains("learned adjacency")));
    Assert.IsTrue(result.Errors.Any(error => error.Contains("violates constraint")));
  }

  [TestMethod]
  public void SkipsSerializedZeroCells()
  {
    int[,] tiles = new int[2, 1];

    Map_Validation_Result result = Map_Constraint_Validator.validate(
      tiles,
      create_data());

    Assert.IsTrue(result.Is_Valid);
    Assert.AreEqual(2, result.Skipped_Zero_Cell_Count);
    Assert.AreEqual(0, result.Checked_Adjacency_Count);
  }

  [TestMethod]
  public void ExperimentalValidationRequiresMutualAdjacency()
  {
    Tileset_Generation_Data data = create_data();
    data.generation_data[2].Valid_Tile_Priority[(byte) 4].Clear();
    int[,] tiles = new int[2, 1]
    {
      { 1 },
      { 2 }
    };

    Map_Validation_Result legacy = Map_Constraint_Validator.validate(tiles, data);
    Map_Validation_Result experimental = Map_Constraint_Validator.validate(
      tiles,
      data,
      algorithm: Map_Generation_Algorithm.Experimental_Constraint);

    Assert.IsTrue(legacy.Is_Valid);
    Assert.IsFalse(experimental.Is_Valid);
  }

  private static Tileset_Generation_Data create_data()
  {
    Tileset_Generation_Data data = new Tileset_Generation_Data(
      0,
      new Tile_Matching_Data(new HashSet<Tile_Directions>()));
    data.generation_data[1] = new Tile_Data();
    data.generation_data[2] = new Tile_Data();
    data.generation_data[1].Valid_Tile_Priority[(byte) 6][(short) 2] = 1;
    data.generation_data[2].Valid_Tile_Priority[(byte) 4][(short) 1] = 1;
    return data;
  }
}
