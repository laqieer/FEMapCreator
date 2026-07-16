using FEXNA_Library;
using FE_Map_Creator.Generation;

namespace FE_Map_Creator.Tests;

internal static class ExperimentalBranchArcConsistencyTestFixture
{
  internal const string Tileset_Name = "branch-arc-fixture";
  internal const short Bad_Root = 1;
  internal const short Good_Root = 66;
  internal const short Fixed_Priority = 67;
  internal const short A0 = 68;
  internal const short A1 = 69;
  internal const short Escape = 70;
  internal const short B0 = 71;
  internal const short B1 = 72;
  internal const short C0 = 73;
  internal const short C1 = 74;
  internal const short D0 = 75;
  internal const short D1 = 76;

  internal static Tileset_Generation_Data create_data()
  {
    Tileset_Generation_Data data = new Tileset_Generation_Data(
      0,
      new Tile_Matching_Data(new HashSet<Tile_Directions>()));
    for (short tile = Bad_Root; tile <= D1; ++tile)
      data.generation_data[tile] = new Tile_Data();
    data.generation_data[Fixed_Priority].Priority = short.MaxValue;

    add_edge(data, Fixed_Priority, 2, Bad_Root, short.MaxValue);
    add_edge(data, Fixed_Priority, 2, Good_Root, 1);
    add_edge(data, Bad_Root, 6, A0, 1);
    add_edge(data, Bad_Root, 6, A1, 1);
    add_edge(data, Good_Root, 6, Escape, 1);
    add_edge(data, A0, 6, B0, 1);
    add_edge(data, A1, 6, B1, 1);
    add_edge(data, Escape, 6, B0, 1);
    add_edge(data, A0, 2, D1, 1);
    add_edge(data, A1, 2, D0, 1);
    add_edge(data, Escape, 2, D0, 1);
    add_edge(data, B0, 2, C0, 1);
    add_edge(data, B1, 2, C1, 1);
    add_edge(data, D0, 6, C0, 1);
    add_edge(data, D1, 6, C1, 1);
    return data;
  }

  internal static Data_Tileset create_metadata()
  {
    List<int> tags = Enumerable.Repeat(99, D1 + 1).ToList();
    tags[0] = 0;
    tags[Bad_Root] = 1;
    tags[Good_Root] = 1;
    tags[Fixed_Priority] = 6;
    tags[A0] = tags[A1] = tags[Escape] = 2;
    tags[B0] = tags[B1] = 3;
    tags[C0] = tags[C1] = 4;
    tags[D0] = tags[D1] = 5;
    return new Data_Tileset() { Terrain_Tags = tags };
  }

  internal static Map_State create_state()
  {
    Map_State state = new Map_State(
      new int[3, 3],
      new bool[3, 3],
      new bool[3, 3],
      new int[3, 3]);
    state.Tiles[0, 0] = Fixed_Priority;
    state.Drawn[0, 0] = true;
    state.Locked[0, 0] = true;
    for (int x = 1; x < 3; ++x)
    {
      state.Tiles[x, 0] = 999;
      state.Drawn[x, 0] = true;
      state.Locked[x, 0] = true;
    }
    state.Tiles[0, 2] = 999;
    state.Drawn[0, 2] = true;
    state.Locked[0, 2] = true;
    state.Terrain[0, 1] = 1;
    state.Terrain[1, 1] = 2;
    state.Terrain[2, 1] = 3;
    state.Terrain[1, 2] = 5;
    state.Terrain[2, 2] = 4;
    return state;
  }

  internal static Map_Job_Spec create_spec()
  {
    Map_State state = create_state();
    return new Map_Job_Spec()
    {
      Version = 1,
      Algorithm = "experimental",
      Width = state.Width,
      Height = state.Height,
      Tileset = Tileset_Name,
      ExperimentalSearchNodeLimit = 20,
      ExperimentalRestartCount = 2,
      ExperimentalNogoodLimit = 64,
      ExperimentalEnableBranchArcConsistency = false,
      Drawn = bool_rows(state.Drawn),
      Locked = bool_rows(state.Locked),
      Terrain = int_rows(state.Terrain)
    };
  }

  internal static string write_assets(string directory)
  {
    Directory.CreateDirectory(directory);
    string generation_data = Path.Combine(directory, $"{Tileset_Name}.dat");
    using (FileStream stream = new FileStream(
      generation_data,
      FileMode.Create,
      FileAccess.Write,
      FileShare.None))
    using (BinaryWriter writer = new BinaryWriter(stream))
      create_data().write(writer);

    string terrain_tags = string.Join(" ", create_metadata().Terrain_Tags);
    File.WriteAllText(
      Path.Combine(directory, "Tileset_Data.xml"),
      $"""
      <?xml version="1.0" encoding="utf-8"?>
      <XnaContent>
        <Asset Type="Generic:Dictionary[int,FEXNA_Library.Data_Tileset]">
          <Item>
            <Key>1</Key>
            <Value>
              <Id>1</Id>
              <Name>{Tileset_Name}</Name>
              <Graphic_Name>{Tileset_Name}</Graphic_Name>
              <Terrain_Tags>{terrain_tags}</Terrain_Tags>
            </Value>
          </Item>
        </Asset>
      </XnaContent>
      """);
    return generation_data;
  }

  internal static void write_template(string filename)
  {
    Map_State state = create_state();
    new Text_Map_Codec().write(
      filename,
      new Map_Document((int[,]) state.Tiles.Clone(), Tileset_Name));
  }

  private static void add_edge(
    Tileset_Generation_Data data,
    short source,
    byte direction,
    short target,
    short weight)
  {
    data.generation_data[source].Valid_Tile_Priority[direction][target] = weight;
    data.generation_data[target].Valid_Tile_Priority[(byte) (10 - direction)][source] = weight;
  }

  private static bool[][] bool_rows(bool[,] values)
  {
    bool[][] rows = new bool[values.GetLength(1)][];
    for (int y = 0; y < rows.Length; ++y)
    {
      rows[y] = new bool[values.GetLength(0)];
      for (int x = 0; x < rows[y].Length; ++x)
        rows[y][x] = values[x, y];
    }
    return rows;
  }

  private static int[][] int_rows(int[,] values)
  {
    int[][] rows = new int[values.GetLength(1)][];
    for (int y = 0; y < rows.Length; ++y)
    {
      rows[y] = new int[values.GetLength(0)];
      for (int x = 0; x < rows[y].Length; ++x)
        rows[y][x] = values[x, y];
    }
    return rows;
  }
}
