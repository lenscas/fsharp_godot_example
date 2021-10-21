namespace super_dungeon_maker

open Godot
open GDUtils
open PollClient
open Client

type JoinButtonFs() as this =
    inherit Control()

    let textBox =
        this.getNode<TextEdit> ("./CodeInserter")

    let mutable poll = None
    member val RunAfterConnected = None with get, set

    override this._Process _ =
        poll
        |> Poll.TryPoll
            (fun x ->
                match (x, this.RunAfterConnected) with
                | Some x, Some func ->
                    func x
                    this.RunAfterConnected <- None
                | None, Some _ -> poll <- None
                | None, None ->
                    GD.Print "no data and no func to run"
                    poll <- None
                | Some _, None -> GD.Print "data but no func to run")
        |> ignore

    member _.ConnectButtonPressed() =
        poll <-
            Login.connectTo "127.0.0.1" false
            |> Poll.AndThen
                (fun _ ->
                    if textBox.Value.Text |> System.String.IsNullOrEmpty then
                        Login.create ()
                    else
                        Login.join textBox.Value.Text
                        |> Poll.MapOk(fun x -> x |> Option.map (fun y -> textBox.Value.Text, y)))
            |> Poll.Map
                (fun x ->
                    match x with
                    | Result.Error x ->
                        GD.PrintErr x
                        GD.PrintStack()
                        None
                    | Ok None -> None
                    | Ok (Some x) -> Some x)
            |> Some
