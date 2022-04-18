[<AutoOpen>]
module TrickyCat.VideoEncoder.Utils

open System.Text.Json
open System.Diagnostics

let notEmpty = System.String.IsNullOrEmpty >> not


let deserialize<'a> (s: string): Result<'a, string> =
    try
        s
        |> JsonSerializer.Deserialize<'a>
        |> Ok
    with
    e -> e.Message |> sprintf "Deserialization failed with: %s" |> Error


let tee f x = f x; x

let const' x _ = x

module ProcessUtils =

    type ProcessTweaker = {
        preStart : Process -> unit
        postStart: Process -> unit
    }

    let emptyTweaker = { preStart = ignore; postStart = ignore }

    let preStartTweaker preStart = { emptyTweaker with preStart = preStart }

    let postStartTweaker postStart = { emptyTweaker with postStart = postStart }


    let private createProcess (startInfo: ProcessStartInfo) : Process =
        let process' = new Process()
        process'.StartInfo <- startInfo
        process'


    let private getProcessOutput (process': Process) : Result<string, string> =
        try
            let output = process'.StandardOutput.ReadToEnd()
            Ok output
        with
        | e ->
            e.Message
            |> sprintf "Output retrieval failed for process [ %s %s ] with: %s"
                    process'.StartInfo.FileName
                    process'.StartInfo.Arguments
            |> Error


    let private getStartInfo cmd args =
        ProcessStartInfo(
            cmd,        
            args,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        )


    let runProcessSuccessfully processTweaker (startInfo: ProcessStartInfo) : Result<Process, string> =        
        try
            let process' = createProcess startInfo
            processTweaker.preStart process'
            process'.Start() |> ignore
            processTweaker.postStart process'
            process'.WaitForExit()

            if process'.ExitCode <> 0 then
                let stdErr = process'.StandardError.ReadToEnd()
                
                if notEmpty stdErr 
                then sprintf "Process error %i: %s" process'.ExitCode stdErr 
                else sprintf "Unknown process error %i" process'.ExitCode
                |> Error            
            else
                Ok process'
        with
        | ex ->
            ex.Message
            |> sprintf "Process [ %s %s ] failed with: %s" startInfo.FileName startInfo.Arguments
            |> Error


    let runProcessSuccessfully' processTweaker cmd args =
        getStartInfo cmd args
        |> runProcessSuccessfully processTweaker


    let runAndGetProcessOutput processTweaker cmd args : Result<string, string> =

        getStartInfo cmd args
        |> runProcessSuccessfully processTweaker
        >>= getProcessOutput


    let runAndGetNonEmptyProcessOutput processTweaker cmd args : Result<string, string> =
    
        runAndGetProcessOutput processTweaker cmd args
        >>= (fun s -> if notEmpty s then Ok s else Error "Empty process output.")