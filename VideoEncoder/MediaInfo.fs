module TrickyCat.VideoEncoder.MediaInfo

open System.IO


type Dimensions = {
    width : int
    height: int
}

type MediaInfoError = MediaInfoError of string

type private FfprobeQueryKind = AudioStreamQuery | VideoStreamQuery

type private AudioStreamDto = {|
    codec_type: string
    duration: string
|}

type private VideoStreamDto = {|
    codec_type: string
    width: int
    height: int
    duration: string
|}

type private StreamsDto<'streamDto> = {|
    streams: 'streamDto array
|}

type private AudioStream = { durationSec: double }

type private VideoStream = { durationSec: double; width: int; height: int }


let private isNonEmptyFile filePath : Result<bool, string> =
    try
        let fileInfo = FileInfo(filePath)
        Ok (fileInfo.Exists && fileInfo.Length > 0)
    with
    | e -> e.Message |> sprintf "Non-emptiness check failed for file [ %s ] with error: %s" filePath |> Error


let private fileExists filePath : Result<string, string> = result {
    let! fileIsNonEmpty = isNonEmptyFile filePath
    return! if fileIsNonEmpty
        then Ok filePath 
        else filePath |> sprintf "File [ %s ] is absent or empty" |> Error
}


let private ``build ffprobe query`` queryKind filePath =
    let streamSpecifier = 
        match queryKind with
        | AudioStreamQuery -> "a:0"
        | VideoStreamQuery -> "v:0"
        
    sprintf "-hide_banner -loglevel 0 -print_format json -show_streams -select_streams %s \"%s\""
        streamSpecifier
        filePath


let private ``run ffprobe query`` ffprobeQuery : Result<string, string> =

    ProcessUtils.runAndGetNonEmptyProcessOutput
        ProcessUtils.emptyTweaker
        "ffprobe"
        ffprobeQuery


let private deserialize'<'a> =

    deserialize<'a>
    >> Result.mapError (sprintf "ffprobe DTO deserialization failed with: %s")


let private getFirstStream<'a> (x : StreamsDto<'a>) =
    if x.streams.Length <> 0 then
        Ok x.streams[0]
    else
        x
        |> sprintf "'%s' array is empty: %A" (nameof(x.streams))
        |> Error
    

let private mapAudioStream (dto: AudioStreamDto) =
    if dto.codec_type = "audio" then
        try
            Ok
                {
                    AudioStream.durationSec = System.Double.Parse dto.duration
                }
        with
        | e -> e.Message |> sprintf "Conversion failed for %A with error: %s" dto |> Error
    else
        dto
        |> sprintf "Not an audio stream: %A"
        |> Error

    
let private mapVideoStream (dto: VideoStreamDto) =
    if dto.codec_type = "video" then
        try
            Ok
                {
                    VideoStream.durationSec = System.Double.Parse dto.duration
                    width = dto.width
                    height = dto.height
                }
        with
        | e -> e.Message |> sprintf "Conversion failed for %A with error: %s" dto |> Error
    else
        dto
        |> sprintf "Not the video stream: %A"
        |> Error


let private getStream<'streamDto> queryKind =

    ``build ffprobe query`` queryKind
    >> ``run ffprobe query``
    >=> deserialize'<StreamsDto<'streamDto>>
    >=> getFirstStream


let private getAudioStream : string -> Result<AudioStream, string> =
        
    getStream<AudioStreamDto> AudioStreamQuery
    >=> mapAudioStream


let private getVideoStream : string -> Result<VideoStream, string> =

    getStream<VideoStreamDto> VideoStreamQuery
    >=> mapVideoStream


let private getDurationInSeconds filePath: Result<double, string> = result {
    let! audio = getAudioStream filePath
    let! video = getVideoStream filePath

    return min audio.durationSec video.durationSec
}


let private mapDimensions videoStream =
    {
        Dimensions.width = videoStream.width
        height = videoStream.height
    }

let private getDimensions' : string -> Result<Dimensions, string> =
    fileExists
    >=> getVideoStream
    >> Result.map mapDimensions


let private targetFileHasSameDimensions sourceFilePath targetFilePath = result {
    let! sourceDimensions = getDimensions' sourceFilePath
    let! targetDimensions = getDimensions' targetFilePath
    return sourceDimensions = targetDimensions
}

let private targetFileHasSameDuration sourceFilePath targetFilePath = result {
    let! sourceDuration = getDurationInSeconds sourceFilePath
    let! targetDuration = getDurationInSeconds targetFilePath
    let dx = sourceDuration - targetDuration

    return abs dx < 0.1
}

let private targetFileIsNonEmpty _ targetFilePath = isNonEmptyFile targetFilePath


open BoolResultUtils
let private targetMediaFileAlreadyExists' =
    targetFileIsNonEmpty
    <&&> targetFileHasSameDimensions
    <&&> targetFileHasSameDuration


let targetMediaFileAlreadyExists sourceFilePath targetFilePath : Result<bool, MediaInfoError> =
    targetMediaFileAlreadyExists' sourceFilePath targetFilePath
    |> Result.mapError MediaInfoError


let getDimensions : string -> Result<Dimensions, MediaInfoError> =        
    getDimensions'
    >> Result.mapError MediaInfoError
