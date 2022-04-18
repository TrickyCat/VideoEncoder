module TrickyCat.VideoEncoder.ArgsProvider

module private ConsoleArgs =

    let private getArgByIdx idx (args: string array) =
        if idx < args.Length then
            Ok args[idx]
        else
            idx
            |> sprintf "No argument with idx = %i exist"
            |> Error

    let getSourceFolderFromArgs = getArgByIdx 0

    let getTargetFolderFromArgs = getArgByIdx 1


module private EnvArgs =

    let private envPrefix           = "VideoEncoder_"
    let private sourceFolderEnvName = "SourceFolder"
    let private targetFolderEnvName = "TargetFolder"

    let private getEnvVarFullName name = sprintf "%s%s" envPrefix name

    let private getEnvVar name =
        try
            let envVal = System.Environment.GetEnvironmentVariable name
            if notEmpty envVal then
                Ok envVal
            else
                name |> sprintf "Env var '%s' is empty" |> Error
        with
        | e ->
            e.Message
            |> sprintf "Reading of env var '%s' failed with: %s" name
            |> Error


    let sourceEnvVarFullName = getEnvVarFullName sourceFolderEnvName

    let targetEnvVarFullName = getEnvVarFullName targetFolderEnvName

    let getSourceFolderFromEnv _ = getEnvVar sourceEnvVarFullName

    let getTargetFolderFromEnv _ = getEnvVar targetEnvVarFullName


open ConsoleArgs
open EnvArgs
    
let printProgramUsage _ =
    printfn """Program usage:

1) CMD <sourceDir> <targetDir>

or

2) CMD

   In this case the runtime arguments will be read from the following environment variables:

   - %s
   - %s

If both the command arguments and environment variables are specified when 
the command arguments are being used.

3) You can mix the argument sources:

   set %s=/destination

   dotnet VideoEncoder.dll /source-1
   dotnet VideoEncoder.dll /source-2
   dotnet VideoEncoder.dll /source-3

External dependencies:

- ffmpeg
- ffprobe
    """
        sourceEnvVarFullName
        targetEnvVarFullName
        targetEnvVarFullName


let private getSourceFolder = getSourceFolderFromArgs <||> getSourceFolderFromEnv

let private getTargetFolder = getTargetFolderFromArgs <||> getTargetFolderFromEnv

type AppOptions = {
    sourceFolder: string
    targetFolder: string
}

let getOptions args = result {
    let! sourceFolder = getSourceFolder args
    let! targetFolder = getTargetFolder args

    return {
        sourceFolder = sourceFolder
        targetFolder = targetFolder
    }
}