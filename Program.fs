open System.IO

open FSharpx
open MessagePack
open MessagePack.FSharp
open MessagePack.Resolvers
open System.Diagnostics


let resolver =
  Resolvers.CompositeResolver.Create(
    FSharpResolver.Instance,
    StandardResolver.Instance,
    DynamicObjectResolver.Instance
)
let options = MessagePackSerializerOptions.Standard.WithResolver(resolver)

[<MessagePackObject>]
type RoundTripData = 
  {
    [<MessagePack.Key 0>] InputMatrix : float array array
    [<MessagePack.Key 1>] Weights : float array
  }

module RoundTripData = 
  let toMessagePack (msg:RoundTripData) =
    MessagePackSerializer.Serialize(msg, options)

  let fromMessagePack (msg:byte[]) =
    Result.protect (fun _ -> MessagePackSerializer.Deserialize<RoundTripData>(msg, options)) ()


let inputs = 
  {
    InputMatrix = [| [| 1.0; 2.0; 3.0 |]; [| 4.0; 5.0; 6.0 |] |]
    Weights = [| 0.1; 0.2; 0.3 |]
  }


let proc = 
  Process.Start(
    ProcessStartInfo(
      "conda",
      "run -n hourly-model-training --no-capture-output python model.py",
      UseShellExecute = false,
      RedirectStandardInput = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true
    )
  )

proc.Start() |> ignore

let bytes = RoundTripData.toMessagePack inputs
let writer = new BinaryWriter(proc.StandardInput.BaseStream)
writer.Write bytes
writer.Close ()

let err = proc.StandardError.ReadToEnd()

let readAllBytes (stream:Stream) =
  use ms = new MemoryStream()
  do stream.CopyTo(ms)
  ms.ToArray()

let output =
  let bytes = readAllBytes proc.StandardOutput.BaseStream
  RoundTripData.fromMessagePack bytes

printfn $"%A{output}"
if err.Length > 0 then
  failwith $"got this err: %s{err}"


