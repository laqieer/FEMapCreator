using System.Threading;
using System.Threading.Tasks;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Narrow seam between the command-line surface and the map generation/repair engine.
/// Command actions only bind options, validate them, and call one of these methods;
/// they never implement generation logic themselves. <see cref="Core_Cli_Executor"/>
/// provides the production implementation backed by <c>FE_Map_Creator.Core</c>.
/// </summary>
internal interface ICli_Executor
{
  Task<Cli_Execution_Result> generate_async(
    Generate_Request request,
    Cli_Output output,
    CancellationToken cancellation_token);

  Task<Cli_Execution_Result> repair_async(
    Repair_Request request,
    Cli_Output output,
    CancellationToken cancellation_token);

  Task<Cli_Execution_Result> batch_async(
    Batch_Request request,
    Cli_Output output,
    CancellationToken cancellation_token);

  Task<Cli_Execution_Result> tilesets_list_async(
    Tilesets_List_Request request,
    Cli_Output output,
    CancellationToken cancellation_token);

  Task<Cli_Execution_Result> validate_async(
    Validate_Request request,
    Cli_Output output,
    CancellationToken cancellation_token);
}
