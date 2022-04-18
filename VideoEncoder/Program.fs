module TrickyCat.VideoEncoder.Main

open ArgsProvider
open EncodingService

[<EntryPoint>]
let main (args: string array) =

    args
    |> getOptions
    >>= (fun { sourceFolder = source; targetFolder = target } -> 
            encode
                (printfn "%s")
                target
                source
        )
    |> Result.map (const' 0)
    |> Result.mapError (printProgramUsage >> const' 1)
    |> (function Ok code -> code | Error code -> code)