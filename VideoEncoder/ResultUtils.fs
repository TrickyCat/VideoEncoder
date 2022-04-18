[<AutoOpen>]
module TrickyCat.VideoEncoder.ResultUtils

open System

let (>>=) x f = match x with
                | Ok x    -> f x
                | Error e -> Error e


let (>=>) f g x = f x >>= g
    

(*
Kind of an OR combinator for Kleisli arrows:

If a call to 'f' gives an Ok then use that result data.
If a call to 'f' gives an Error then call 'g' for result data.
*)
let (<||>)
    (f: 'a -> Result<'b, 'c>)
    (g: 'a -> Result<'b, 'c>)
    x =
    match f x with
    | Ok a    -> Ok a
    | Error _ -> g x


let ofOption error = function Some s -> Ok s | None -> Error error

let combineAll results =
    results
    |> Seq.fold (fun state result -> 
        match state, result with
        | Ok acc, Ok res -> Ok <| res :: acc
        | Error e, _
        | _, Error e -> Error e
        ) (Ok [])


type ResultBuilder() =
    member __.Return(x) = Ok x

    member __.ReturnFrom(m: Result<_, _>) = m

    member __.Bind(m, f) = Result.bind f m
    member __.Bind((m, error): (Option<'T> * 'E), f) = m |> ofOption error |> Result.bind f

    member __.Zero() = None

    member __.Combine(m, f) = Result.bind f m

    member __.Delay(f: unit -> _) = f

    member __.Run(f) = f()

    member __.TryWith(m, h) =
        try __.ReturnFrom(m)
        with e -> h e

    member __.TryFinally(m, compensation) =
        try __.ReturnFrom(m)
        finally compensation()

    member __.Using(res: #IDisposable, body) =
        __.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())

    member __.While(guard, f) =
        if not (guard()) then Ok () else
        do f() |> ignore
        __.While(guard, f)

    member __.For(sequence:seq<_>, body) =
        __.Using(sequence.GetEnumerator(), fun enum -> __.While(enum.MoveNext, __.Delay(fun () -> body enum.Current)))

let result = new ResultBuilder()


module BoolResultUtils =

    let (<&&>) 
        (f: 'a -> 'b -> Result<bool, 'e>)
        (g: 'a -> 'b -> Result<bool, 'e>)
        x y =
            match f x y with
            | Ok true -> g x y
            | Ok _    -> Ok false
            | Error e -> Error e