namespace super_dungeon_maker

open Godot
open GDUtils

type EditorFs() as this =
    inherit Node2D()

    let hand = this.getNode<HandFs> "./Hand"
    let bonusBar = this.getNode<BonusBarFs> "./BonusBar"
    let onDoneButton = this.getNode<Button> "./DoneButton"

    override _._Ready() =
        hand.Value.UpdateCostBar <-
            float
            >> bonusBar.Value.SetTo
            >> bonusBar.Value.GetCurrentState
            >> function
                | NotEnough -> onDoneButton.Value.Disabled <- true
                | _ -> onDoneButton.Value.Disabled <- false

    member val FuncAfterDone = None with get, set

    member this.onDoneButtonPressed() =
        match this.FuncAfterDone with
        | Some x -> () |> hand.Value.GetMap |> x
        | None -> ()
