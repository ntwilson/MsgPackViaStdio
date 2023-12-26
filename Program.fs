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

let readAllBytes (reader:BinaryReader) =
  let bufferSize = 4096
  use ms = new MemoryStream()
  let buffer = Array.create bufferSize 0uy
  let mutable count = 0
  count <- reader.Read(buffer, 0, bufferSize)
  while (count <> 0) do
    ms.Write(buffer, 0, count)
    count <- reader.Read(buffer, 0, bufferSize)

  ms.ToArray()

let bytes = RoundTripData.toMessagePack inputs
let writer = new BinaryWriter(proc.StandardInput.BaseStream)
writer.Write bytes
writer.Close ()

let err = proc.StandardError.ReadToEnd()
let output =
  use reader = new BinaryReader(proc.StandardOutput.BaseStream)
  let bytes = readAllBytes reader
  RoundTripData.fromMessagePack bytes

printfn $"%A{output}"
if err.Length > 0 then
  failwith $"got this err: %s{err}"


