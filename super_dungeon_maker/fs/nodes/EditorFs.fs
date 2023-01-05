namespace super_dungeon_maker

open Godot
open GDUtils

open GodotTypeProvider

type EditorScene = GodotTypeProvider.CreateRootNode<Test.GodotProj.Editor>

// type TestFS() = 
    // inherit EditorScene()
// 
    // member _.test() = GD.Print("nice")

type EditorFs() as this =
    inherit Node2D()

    let hand = this.getNode<HandFs> "./Hand"
    let bonusBar = this.getNode<BonusBarFs> "./BonusBar"
    let onDoneButton = this.getNode<Button> "./DoneButton"

    override this._Ready() =
        GD.Print(EditorScene.debugProperty)
        //GD.Print this.Hand.Value
        hand.Value.UpdateCostBar <-
            float
            >> bonusBar.Value.SetTo
            >> bonusBar.Value.GetCurrentState
            >> (=) NotEnough
            >> fun x -> x || () |> hand.Value.HasPlacedStart |> not
            >> fun notEnough -> onDoneButton.Value.Disabled <- notEnough

    member val FuncAfterDone = None with get, set

    member this.onDoneButtonPressed() =
        match this.FuncAfterDone with
        | Some x -> () |> hand.Value.GetMap |> x
        | None -> ()
