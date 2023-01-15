namespace super_dungeon_maker

open Godot
open GDUtils

type AfterDone = System.Collections.Generic.Dictionary<(int * int),Block> -> unit

type EditorFs() =
    inherit EditorNodeScene<AfterDone>()

    override this.Setup (a) = this.FuncAfterDone <- a

    override this._Ready() =
        this.Hand.Value.UpdateCostBar <-
            float
            >> this.BonusBar.Value.SetTo
            >> this.BonusBar.Value.GetCurrentState
            >> (=) NotEnough
            >> fun x -> x || () |> this.Hand.Value.HasPlacedStart |> not
            >> fun notEnough -> this.DoneButton.UnwrappedNode.Disabled <- notEnough

    member val FuncAfterDone = ignore with get, set

    member this.onDoneButtonPressed() =
        () |> this.Hand.Value.GetMap |> this.FuncAfterDone 
