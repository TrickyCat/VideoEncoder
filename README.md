# Video Encoder App

It's a simple helper app that recodes video clips while trying to reduce their size in bytes. 

---

## Quick facts:

- Written in F# for .NET 6

- Under the hood, it utilizes the power of `FFmpeg`

- The app preserves the video resolution of each individual source clip: no upscales or downscales of any kind are performed

- From my experience, sometimes the size is optimized even by a factor of ten: `36 GB` -> `3.5 GB` but that obviously greatly depends on the encoding of the source video clips

- Video quality of the recoded video clips is visually comparable to the one of the source clips.

  **Note:** 
  
  The ultimate use case for this app is to do a batch recoding of the learning video courses and **NOT** to do recoding of `100+ GB` BluRay rips with advanced color spaces since those would require different tunings and parallel processing at scale.

- There's a **"skip encoding"** functionality that allows you to resume the folder processing after an interruption, the file will be skipped if all of the following conditions will be met:

  - the non-empty target file with the expected name already exists in the destination location

  - the dimensions of that target file media match the dimensions of the corresponding source file media

  - the duration of that target file media match the duration of the corresponding source file media

---

## Insights and Usage:

The app accepts 2 arguments:

- source folder path
- target folder path

It then finds all the `*.mp4` files in the source folder and attempts to recode them while placing the recoded output files in the target folder.

Arguments can be passed as:

- positional command-line arguments:

  ```bash
  dotnet VideoEncoder.dll /path/to/source/folder /path/to/target/folder
  ```

- environment variables

  ```powershell
  set VideoEncoder_SourceFolder=/path/to/source/folder
  set VideoEncoder_TargetFolder=/path/to/target/folder

  dotnet VideoEncoder.dll
  ```

- the mix of both:

  ```powershell
  # This example allows you to pin the target folder.
  #
  # Thus each distinct call to the app would have only
  # the varying source folder argument in the command line.

  set VideoEncoder_TargetFolder=/path/to/target/folder

  dotnet VideoEncoder.dll /path/to/first/source/folder
  dotnet VideoEncoder.dll /path/to/second/source/folder
  dotnet VideoEncoder.dll /path/to/third/source/folder
  ```

  **Note:**

  If both arguments would be specified in the command line args list and inside the environment variables then the command line args values would be used.

---

## Simple Usage With Docker Container

There's a Docker container image `trickycat/videoencoder` that contains the app itself as well as the necessary dependencies, thus the usage is more straightforward.

Check the [releases page in this repository](https://github.com/TrickyCat/VideoEncoder/releases) for specific image version numbers or the [Docker Hub](https://hub.docker.com/r/trickycat/videoencoder).

### Examples

```powershell
# It's Powershell
#
# Bash has another delimiter for multiline commands: \

docker run `
    -v //c/src:/src `
    -v //c/dst:/dst `
    --name videoencoder `
    --rm `
    trickycat/videoencoder:0.1.0 /src /dst

###############################################

docker run `
    -v //c/src:/src `                        # pass the folder with source clips as a volume
    -v //c/dst:/dst `                        # pass the output folder for recoded files
    --name videoencoder `                    # name the container for convenience
    --rm `                                   # remove the container after completion
    --env VideoEncoder_TargetFolder=/dst `   # pass an argument as the environment variable
    trickycat/videoencoder /src

###############################################

docker run `
    -v //c/src:/src `
    -v //c/dst:/dst `
    --name videoencoder `
    --rm `
    --env VideoEncoder_SourceFolder=/src `
    --env VideoEncoder_TargetFolder=/dst `
    trickycat/videoencoder

```

---

## Build Options

- Open the solution in Visual Studio and build it (menu items: ``Build -> Build Solution``)

- Using the .NET CLI

  ```bash
  dotnet build -c Release VideoEncoder.sln
  ```

---

## External Dependencies

- [ffmpeg](https://ffmpeg.org)
- [ffprobe](https://ffmpeg.org/ffprobe.html)

Those can be installed following the instructions on their respective websites. At least at the moment of writing both tools can be installed at once as part of the FFmpeg bundle.