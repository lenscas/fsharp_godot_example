namespace super_dungeon_maker

open Godot
open GDUtils

type BonusState =
    | NotEnough
    | Succeeded
    | WithBonus

type BonusBarFs() as this =
    inherit BonusBarScene()

    let mutable currentValue = 0.

    let update () =
        let left =
            if this.MinimunBar.UnwrappedNode.MaxValue <= currentValue then
                this.MinimunBar.UnwrappedNode.Value <- this.MinimunBar.UnwrappedNode.MaxValue
                currentValue - this.MinimunBar.UnwrappedNode.MaxValue
            else
                this.MinimunBar.UnwrappedNode.Value <- currentValue
                0.

        let left =
            if this.DoneBar.UnwrappedNode.MaxValue <= left then
                this.DoneBar.UnwrappedNode.Value <- this.DoneBar.UnwrappedNode.MaxValue
                left - this.DoneBar.UnwrappedNode.MaxValue
            else
                this.DoneBar.UnwrappedNode.Value <- left
                0.

        if this.Bonus1Bar.UnwrappedNode.MaxValue <= left then
            this.Bonus1Bar.UnwrappedNode.Value <- this.DoneBar.UnwrappedNode.MaxValue
        else
            this.Bonus1Bar.UnwrappedNode.Value <- left

    member public _.AddTo v =
        currentValue <- currentValue + v
        update ()

    member public _.SetTo v =
        currentValue <- v
        update ()

    member public _.GetCurrentState() =

        if this.MinimunBar.UnwrappedNode.MaxValue <= currentValue then
            let left = currentValue - this.MinimunBar.UnwrappedNode.MaxValue

            if this.DoneBar.UnwrappedNode.MaxValue <= left then
                let left = left - this.DoneBar.UnwrappedNode.MaxValue

                if this.Bonus1Bar.UnwrappedNode.MaxValue <= left then
                    WithBonus
                else
                    Succeeded
            else
                NotEnough
        else
            NotEnough
