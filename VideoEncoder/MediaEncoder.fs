module TrickyCat.VideoEncoder.MediaEncoder

open MediaInfo
open System.Diagnostics

type EncodedFilePath = string

type MediaEncoderError = {
    sourceFilePath: string
    targetFilePath: string
    error: string
}

let private getErrorDetails sourceFilePath targetFilePath error =
    {
        sourceFilePath = sourceFilePath
        targetFilePath = targetFilePath
        error = error
    }

let private ``get ffmpeg encode query`` sourceFilePath targetFilePath dimensions =

    $"-i \"{sourceFilePath}\" -loglevel fatal -y -c:v libx264 -crf 24 -pix_fmt yuv420p -tune film -c:a aac -b:a 192k -ar 44100 -vol 300 -strict -2 -speed fastest -s {dimensions.width}x{dimensions.height} \"{targetFilePath}\""


let private ``run ffmpeg query`` ffmpegQuery =

    ProcessUtils.runProcessSuccessfully'
        {
            preStart = (fun process' ->
                process'.StartInfo.RedirectStandardOutput <- false
                process'.StartInfo.RedirectStandardError <- false
            )
            postStart = (fun process' -> 
                process'.PriorityClass <- ProcessPriorityClass.BelowNormal
            )
        }
        
        "ffmpeg"
        ffmpegQuery
    |> Result.map ignore
    

let encodeFile
    (sourceFilePath: string)
    (targetFilePath: string)
    (dimensions: Dimensions)
    : Result<EncodedFilePath, MediaEncoderError> =

    ``get ffmpeg encode query`` sourceFilePath targetFilePath dimensions
    |> ``run ffmpeg query``
    |> Result.map (fun () -> targetFilePath)
    |> Result.mapError (getErrorDetails sourceFilePath targetFilePath)
