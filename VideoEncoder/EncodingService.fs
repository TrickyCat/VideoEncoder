module TrickyCat.VideoEncoder.EncodingService

open System.IO
open MediaInfo
open MediaEncoder

let private searchPatterns = [| "*.mp4" |]


let private getFiles' folderPath searchPattern : Result<string array, string> =
    try
        Ok <| Directory.GetFiles(folderPath, searchPattern, SearchOption.AllDirectories)
    with
    | e ->
        e.Message
        |> sprintf "Getting files (%s) from folder (%s) failed with: %s"
           searchPattern folderPath
        |> Error


let private getFiles log folderPath =
    searchPatterns
    |> Seq.fold
         (fun filesAcc searchPattern -> 
            match getFiles' folderPath searchPattern with
            | Ok fileNames -> fileNames
            | Error e      ->
                log e
                Array.empty<string>
            |> Seq.append filesAcc
            )
         Seq.empty<string>


let private ensureFolderExists folderPath : Result<string, string> =

    if Directory.Exists folderPath then
        Ok folderPath
    else
        folderPath
        |> sprintf "Folder does not exist: %s"
        |> Error
    

let private endsWithSeparatorChar (s: string) =
    if s.EndsWith(Path.DirectorySeparatorChar) then
        s
    else
        sprintf "%s%c" s Path.DirectorySeparatorChar    

let private normalizeFolder (s: string) = Path.TrimEndingDirectorySeparator s

let private getTargetFilePath sourceFolder targetFolder (sourceFilePath: string) =
    let parent = Directory.GetParent(sourceFolder |> normalizeFolder).FullName |> endsWithSeparatorChar
    
    Path.Combine(
        targetFolder,
        sourceFilePath.Substring(parent.Length)
    )
    
    
let private createParentFolders filePath =
    try
        Directory.CreateDirectory(FileInfo(filePath).DirectoryName)
        |> ignore
        |> Ok
    with
    | e -> 
        e.Message
        |> sprintf "Creation of parent folders for file [ %s ] failed with: %s"
           filePath
        |> Error


type private HandleOutcome = Encoded | Skipped

let private mapMediaInfoError<'a> : Result<'a, MediaInfoError> -> Result<'a, string> =

    Result.mapError (function MediaInfoError e -> sprintf "[MediaInfoError]: %s" e)


let private mapMediaEncoderError<'a> : Result<'a, MediaEncoderError> -> Result<'a, string> =

    Result.mapError 
        (fun e ->
            sprintf "[MediaEncoderError]: Source file: [ %s ] Target file: [ %s ] Error: %s"
                e.sourceFilePath e.targetFilePath e.error)


let private targetMediaFileAlreadyExists' sourceFilePath targetFilePath =
    match targetMediaFileAlreadyExists sourceFilePath targetFilePath with
    | Ok ok   -> Ok ok
    | Error _ -> Ok false



let private handleFile' log sourceFolder targetFolder sourceFilePath = result {

    let targetFilePath = getTargetFilePath sourceFolder targetFolder sourceFilePath

    targetFilePath
    |> sprintf "Target file path: %s"
    |> log

    do! createParentFolders targetFilePath
    
    let! targetIsOk = targetMediaFileAlreadyExists' sourceFilePath targetFilePath

    if targetIsOk then
        return Skipped
    else

    let! dimensions = getDimensions sourceFilePath |> mapMediaInfoError

    let! encodedFilePath = encodeFile sourceFilePath targetFilePath dimensions |> mapMediaEncoderError

    return Encoded
}


type private HandlingStat() = class
    let encodedCount = ref 0
    let skippedCount = ref 0
    let failedCount  = ref 0

    member _.encoded () = encodedCount.Value <- encodedCount.Value + 1
    member _.skipped () = skippedCount.Value <- skippedCount.Value + 1
    member _.failed  () = failedCount.Value  <- failedCount.Value  + 1
    
    member _.logTo log =
        sprintf "Handling Stat: encoded = %i skipped = %i failed = %i"
                 encodedCount.Value
                 skippedCount.Value
                 failedCount.Value
        |> log
    end


let private handleFile (stat: HandlingStat) log sourceFolder targetFolder sourceFilePath =
    sourceFilePath
    |> sprintf "Source file path: %s"
    |> log
    
    handleFile' log sourceFolder targetFolder sourceFilePath
    |> Result.map (function 
        | Encoded -> log "Encoded"; stat.encoded() 
        | Skipped -> log "Skipped"; stat.skipped())

    |> Result.mapError (sprintf "Failed: %s" >> log >> stat.failed)
    |> ignore


let encode
    log
    (targetFolder: string)
    (sourceFolder: string)
    :
    Result<unit, string> =

    ensureFolderExists sourceFolder
    |> Result.map 
       (fun sourceFolder ->

        sourceFolder
        |> sprintf "Processing folder: [ %s ]"
        |> log

        let filePaths = getFiles log sourceFolder |> Array.ofSeq
        let n = filePaths.Length
        let nLen = n.ToString().Length

        let stat = HandlingStat()

        filePaths
        |> Array.iteri
           (fun idx sourceFilePath ->

                let prefix = sprintf "[ %0*i / %i ]\n" nLen (idx + 1) n
                log prefix

                handleFile
                    stat
                    (sprintf "%*c%s" prefix.Length ' ' >> log)
                    sourceFolder
                    targetFolder
                    sourceFilePath

                log ""
           )

        stat.logTo log
       )
    |> Result.mapError (tee log)
    


    

    

    